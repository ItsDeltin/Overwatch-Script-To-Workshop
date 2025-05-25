namespace Deltin.Deltinteger.Parse;

using JsonSyntax = Deltin.Deltinteger.Compiler.SyntaxTree.ImportJsonSyntax;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

class ImportJson : IExpression
{
    JsonItem root;

    const string X = "x", Y = "y", Z = "z", A = "a", R = "r", G = "g", B = "b";

    public ImportJson(ParseInfo parseInfo, JsonSyntax syntax)
    {
        root = new JsonItem(parseInfo.Types.Unknown(), JsonItemKind.Unknown, _ => Num(0));

        if (!syntax.File)
            return;

        GetFileContent(parseInfo.Script.Uri, syntax.File.Text.RemoveQuotes())
            .AndThen(rootJtoken => ProcessJtoken(parseInfo, rootJtoken))
            .Match(
                ok => root = ok,
                err => parseInfo.Script.Diagnostics.Error(err, syntax.File.Range)
            );
    }

    Result<JsonElement, string> GetFileContent(Uri uri, string fileName)
    {
        // Get the path.
        string path = Extras.CombinePathWithDotNotation(uri.LocalPath, fileName);
        try
        {
            // Read the file.
            // '/**/' is added as a way to prevent unclosed comments from causing an error,
            // since generated json from the workshop may have an extraneous /*.
            string jsonText = File.ReadAllText(path) + "/**/";
            // Get JSON
            try
            {
                var document = JsonDocument.Parse(jsonText, new()
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                return Result<JsonElement, string>.Ok(document.RootElement);
            }
            catch (Exception jsonException)
            {
                return Result<JsonElement, string>.Error(jsonException.Message);
            }
        }
        catch (Exception ex)
        {
            return Result<JsonElement, string>.Error(ex.Message);
        }
    }

    static Result<JsonItem, string> ProcessJtoken(ParseInfo parseInfo, JsonElement jtoken)
    {
        switch (jtoken.ValueKind)
        {
            // Structs and vectors.
            case JsonValueKind.Object:
                return ProcessJobject(parseInfo, jtoken);

            // Arrays
            case JsonValueKind.Array:
                return ProcessJarray(parseInfo, jtoken);

            // Numbers
            case JsonValueKind.Number:
                return Ok(new JsonItem(parseInfo.Types.Number(), JsonItemKind.Number, actionSet => Num(jtoken.GetDouble())));

            // Booleans
            case JsonValueKind.True:
            case JsonValueKind.False:
                return Ok(new JsonItem(parseInfo.Types.Boolean(), JsonItemKind.Boolean, actionSet => jtoken.GetBoolean() ? True() : False()));

            // Everything else is a string.
            default:
                return Ok(new JsonItem(parseInfo.Types.String(), JsonItemKind.String, actionSet => new StringElement(jtoken.GetString())));
        }
    }

    static Result<JsonItem, string> ProcessJobject(ParseInfo parseInfo, JsonElement jobject)
    {
        // Object is vector?
        if (ComponentObject(parseInfo, jobject, out var vector, X, Y, Z))
        {
            return Ok(new(parseInfo.Types.Vector(), JsonItemKind.Vector, actionSet => Vector(
                vector[0].getValue(actionSet), vector[1].getValue(actionSet), vector[2].getValue(actionSet)
            )));
        }
        // Object is rgba color?
        if (ComponentObject(parseInfo, jobject, out var color, R, G, B, A))
        {
            return Ok(new(parseInfo.Types.Color(), JsonItemKind.Color, actionSet => Element.CustomColor(
                color[0].getValue(actionSet),
                color[1].getValue(actionSet),
                color[2].getValue(actionSet),
                color[3].getValue(actionSet)
            )));
        }
        // Object is workshop constant? (Map, Color, Gamemode, ect.)
        var tryGetWorkshopConstant = CheckAllWorkshopConstantObjects(parseInfo, jobject);
        if (tryGetWorkshopConstant.HasValue)
            return tryGetWorkshopConstant.Value;

        // Create struct from object.
        var structMaker = new StructMaker(parseInfo.TranslateInfo);
        foreach (var property in jobject.EnumerateObject())
        {
            var item = ProcessJtoken(parseInfo, property.Value);

            // Add a new variable to the struct.
            if (item.IsOk)
                structMaker.AddVariable(property.Name, item.Value.type, IVariableDefault.Create(actionSet => item.Value.getValue(actionSet)));
            else // Not ok, return the error.
                return item;
        }
        // Create the struct type.
        var structType = structMaker.GetProvider().GetInstance(InstanceAnonymousTypeLinker.Empty);

        return Ok(new JsonItem(structType, JsonItemKind.Struct, actionSet =>
        {
            var structAssigner = new StructAssigner(
                structType,
                new StructAssigningAttributes(),
                false);

            return structAssigner.GetValues(actionSet);
        }));
    }

    static bool ComponentObject(ParseInfo parseInfo, JsonElement jobject, out JsonItem[] items, params string[] properties)
    {
        var objectProperties = jobject.EnumerateObject().ToArray();
        if (objectProperties.Length == properties.Length && objectProperties.All(p => properties.Contains(p.Name)))
        {
            var processed = properties.Select(p => ProcessJtoken(parseInfo, jobject.GetProperty(p)));

            // Any process tokens have an error?
            // Make sure all values are numbers.
            if (processed.All(p => p.IsOk && p.Value.kind == JsonItemKind.Number))
            {
                items = processed.Select(p => p.Value).ToArray();
                return true;
            }
        }
        items = null;
        return false;
    }

    static Result<JsonItem, string>? CheckAllWorkshopConstantObjects(ParseInfo parseInfo, JsonElement jobject)
    {
        // Check every storable workshop constant
        foreach (var storableEnum in ElementEnum.Storable)
        {
            var result = WorkshopConstantObject(parseInfo, jobject, storableEnum);
            if (result.HasValue)
                return result.Value;
        }
        return null;
    }

    static Result<JsonItem, string>? WorkshopConstantObject(ParseInfo parseInfo, JsonElement jobject, string enumIdentifier)
    {
        // Only try to return a workshop constant if there is only one property with the correct name with a string value.
        string keyName = enumIdentifier.ToLower();
        var objectProperties = jobject.EnumerateObject().ToArray();
        if (objectProperties.Length != 1 || !jobject.TryGetProperty(keyName, out var prop) || prop.ValueKind != JsonValueKind.String)
            return null;

        var value = jobject.GetProperty(keyName).GetString();
        var workshopEnum = ElementRoot.Instance.GetEnum(enumIdentifier);
        var workshopValue = workshopEnum.Members.FirstOrDefault(m => m.Name == value)?.ToElement();

        if (workshopValue == null)
            return Error($"\"{value}\" is not a {enumIdentifier} value.\nValid values are:\n{string.Join("\n", workshopEnum.Members.Select(m => $"\"{m.Name}\""))}");

        var type = parseInfo.Types.EnumType(enumIdentifier);

        return Ok(new(type, JsonItemKind.Constant, actionSet => workshopValue));
    }

    static Result<JsonItem, string> ProcessJarray(ParseInfo parseInfo, JsonElement jarray)
    {
        CodeType arrayOfType = null;
        var input = jarray.EnumerateArray().ToArray();
        var arrayItems = new JsonItem[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            var result = ProcessJtoken(parseInfo, input[i]);

            if (!result.IsOk)
                return result;

            arrayItems[i] = result.Value;

            if (arrayOfType == null)
                arrayOfType = arrayItems[i].type;
            // Array is a mix of values, switch to the Any type.
            // Using union types could be a better solution.
            else if (!arrayOfType.Is(arrayItems[i].type))
                arrayOfType = parseInfo.Types.Any();

            // A struct was mixed in with normal objects.
            // This can be changed once unparalleled structs are ready.
            if (i != 0 && (arrayItems[0].kind == JsonItemKind.Struct) != (arrayItems[i].kind == JsonItemKind.Struct))
            {
                return Error("Arrays mixing both objects and values is currently not allowed.");
            }
        }

        // No elements, array is any type.
        arrayOfType ??= parseInfo.Types.Any();

        return Ok(new JsonItem(new ArrayType(parseInfo.Types, arrayOfType), JsonItemKind.Array, actionSet =>
        {
            return StructHelper.CreateArray(arrayItems.Select(a => a.getValue(actionSet)).ToArray());
        }));
    }

    static Result<JsonItem, string> Ok(JsonItem item) => Result<JsonItem, string>.Ok(item);
    static Result<JsonItem, string> Error(string error) => Result<JsonItem, string>.Error(error);

    record struct JsonItem(CodeType type, JsonItemKind kind, Func<ActionSet, IWorkshopTree> getValue);

    public IWorkshopTree Parse(ActionSet actionSet) => root.getValue(actionSet);

    public CodeType Type() => root.type;

    enum JsonItemKind
    {
        Unknown,
        Struct,
        Array,
        Number,
        Boolean,
        Vector,
        Color,
        Constant,
        String,
    }

    class StructMaker
    {
        string structName = "{ ";
        readonly List<IVariable> variables = new();
        readonly List<IVariable> inlineVariables = new();
        readonly List<IVariable> staticVariables = new();
        readonly List<IMethodProvider> methods = new();
        readonly List<AnonymousType> genericTypes = new();
        readonly DeltinScript deltinScript;

        public StructMaker(DeltinScript deltinScript) => this.deltinScript = deltinScript;

        public void AddVariable(string name, CodeType type, IVariableDefault value)
        {
            var newVariable = VariableMaker.New(name, type, value);
            variables.Add(newVariable);

            if (variables.Count > 1)
                structName += ", ";
            structName += newVariable.GetDefaultInstance(null).GetLabel(deltinScript);
        }
        // public void AddVariable(IVariable variable) => variables.Add(variable);
        // public void AddStaticVariable(IVariable variable) => staticVariables.Add(variable);
        // public void AddMethod(IMethodProvider method) => methods.Add(method);
        // public void AddGenericType(AnonymousType type) => genericTypes.Add(type);

        public IStructProvider GetProvider() => new StructProvider(structName + " }", variables.ToArray(), staticVariables.ToArray(), inlineVariables, methods.ToArray(), genericTypes.ToArray());

        record StructProvider(
            string Name,
            IVariable[] Variables,
            IVariable[] StaticVariables,
            IEnumerable<IVariable> InstanceInlineVariables,
            IMethodProvider[] Methods,
            AnonymousType[] GenericTypes) : IStructProvider
        {
            public bool Parallel => true;

            public void DependContent() { }
            public void DependMeta() { }
            public StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker) => new StructInstance(this, typeLinker);
        }
    }
}