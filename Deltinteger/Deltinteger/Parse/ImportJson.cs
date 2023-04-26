namespace Deltin.Deltinteger.Parse;
using JsonSyntax = Deltin.Deltinteger.Compiler.SyntaxTree.ImportJsonSyntax;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

class ImportJson : IExpression
{
    JsonItem root;

    const string X = "x";
    const string Y = "y";
    const string Z = "z";

    public ImportJson(ParseInfo parseInfo, JsonSyntax syntax)
    {
        root = new JsonItem(parseInfo.Types.Unknown(), JsonItemKind.Unknown, _ => Num(0));

        if (!syntax.File)
            return;

        GetFileContent(parseInfo.Script.Uri, syntax.File.Text.RemoveQuotes())
            .AndThen(rootJobject => ProcessJobject(parseInfo, rootJobject))
            .Match(
                ok => root = ok,
                err => parseInfo.Script.Diagnostics.Error(err, syntax.File.Range)
            );
    }

    Result<JObject, string> GetFileContent(Uri uri, string fileName)
    {
        // Get the path.
        string path = Extras.CombinePathWithDotNotation(uri.LocalPath, fileName);
        try
        {
            // Read the file.
            string jsonText = File.ReadAllText(path);
            // Get JSON
            try
            {
                return Result<JObject, string>.Ok(JObject.Parse(jsonText));
            }
            catch (JsonReaderException jsonException)
            {
                return Result<JObject, string>.Error(jsonException.Message);
            }
        }
        catch (Exception ex)
        {
            return Result<JObject, string>.Error(ex.Message);
        }
    }

    static Result<JsonItem, string> ProcessJtoken(ParseInfo parseInfo, JToken jtoken)
    {
        switch (jtoken)
        {
            // Structs and vectors.
            case JObject jobject:
                return ProcessJobject(parseInfo, jobject);
            // Arrays
            case JArray jarray:
                return ProcessJarray(parseInfo, jarray);
        }
        switch (jtoken.Type)
        {
            // Number
            case JTokenType.Float:
            case JTokenType.Integer:
                return Ok(new JsonItem(parseInfo.Types.Number(), JsonItemKind.Number, actionSet => Num(jtoken.ToObject<double>())));
            // Everything else is a string.
            default:
                return Ok(new JsonItem(parseInfo.Types.String(), JsonItemKind.String, actionSet => new StringElement(jtoken.ToObject<string>())));
        }
    }

    static Result<JsonItem, string> ProcessJobject(ParseInfo parseInfo, JObject jobject)
    {
        // Object is vector?
        if (jobject.Count == 3 && jobject.ContainsKey(X) && jobject.ContainsKey(Y) && jobject.ContainsKey(Z))
        {
            // Get all children.
            var xr = ProcessJtoken(parseInfo, jobject[X]);
            var yr = ProcessJtoken(parseInfo, jobject[Y]);
            var zr = ProcessJtoken(parseInfo, jobject[Z]);

            // Make sure x, y, and z did not return an error.
            var xyz = xr.And(yr).And(zr);
            if (!xyz.IsOk)
                return Error(xyz.Err);

            var ((x, y), z) = xyz.Value;

            // Make sure the x, y, and z values are numbers.
            if (x.kind == JsonItemKind.Number && y.kind == JsonItemKind.Number && z.kind == JsonItemKind.Number)
                return Ok(new JsonItem(parseInfo.Types.Vector(), JsonItemKind.Vector, actionSet =>
                {
                    return Vector(x.getValue(actionSet), y.getValue(actionSet), z.getValue(actionSet));
                }));
        }

        // Create struct from object.
        var structMaker = new StructMaker(parseInfo.TranslateInfo);
        foreach (var property in jobject.Properties())
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

    static Result<JsonItem, string> ProcessJarray(ParseInfo parseInfo, JArray jarray)
    {
        CodeType arrayOfType = null;
        var arrayItems = new JsonItem[jarray.Count];
        for (int i = 0; i < jarray.Count; i++)
        {
            var result = ProcessJtoken(parseInfo, jarray[i]);

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
        if (arrayOfType == null)
            arrayOfType = parseInfo.Types.Any();

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
        Vector,
        String,
    }

    class StructMaker
    {
        string structName = "{ ";
        readonly List<IVariable> variables = new();
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

        public IStructProvider GetProvider() => new StructProvider(structName + " }", variables.ToArray(), staticVariables.ToArray(), methods.ToArray(), genericTypes.ToArray());

        record StructProvider(
            string Name,
            IVariable[] Variables,
            IVariable[] StaticVariables,
            IMethodProvider[] Methods,
            AnonymousType[] GenericTypes) : IStructProvider
        {
            public void DependContent() { }
            public void DependMeta() { }
            public StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker) => new StructInstance(this, typeLinker);
        }
    }
}