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

        GetFileContent(parseInfo.Script.Uri, syntax.File.Text.RemoveQuotes()).Match(
            ok =>
            {
                root = ProcessJobject(parseInfo, ok);
            },
            err =>
            {
                parseInfo.Script.Diagnostics.Error(err, syntax.File.Range);
            }
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

    JsonItem ProcessJtoken(ParseInfo parseInfo, JToken jtoken)
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
                return new JsonItem(parseInfo.Types.Number(), JsonItemKind.Number, actionSet => Num(jtoken.ToObject<double>()));
            // Everything else is a string.
            default:
                return new JsonItem(parseInfo.Types.String(), JsonItemKind.String, actionSet => new StringElement(jtoken.ToObject<string>()));
        }
    }

    JsonItem ProcessJobject(ParseInfo parseInfo, JObject jobject)
    {
        // Object is vector?
        if (jobject.Count == 3 && jobject.ContainsKey(X) && jobject.ContainsKey(Y) && jobject.ContainsKey(Z))
        {
            var x = ProcessJtoken(parseInfo, jobject[X]);
            var y = ProcessJtoken(parseInfo, jobject[Y]);
            var z = ProcessJtoken(parseInfo, jobject[Z]);

            if (x.kind == JsonItemKind.Number && y.kind == JsonItemKind.Number && z.kind == JsonItemKind.Number)
                return new JsonItem(parseInfo.Types.Vector(), JsonItemKind.Vector, actionSet =>
                {
                    return Vector(x.getValue(actionSet), y.getValue(actionSet), z.getValue(actionSet));
                });
        }

        var structMaker = new StructMaker(parseInfo.TranslateInfo);
        foreach (var property in jobject.Properties())
        {
            var item = ProcessJtoken(parseInfo, property.Value);
            structMaker.AddVariable(property.Name, item.type, IVariableDefault.Create(actionSet => item.getValue(actionSet)));
        }
        var structType = structMaker.GetProvider().GetInstance(InstanceAnonymousTypeLinker.Empty);

        return new JsonItem(structType, JsonItemKind.Struct, actionSet =>
        {
            var structAssigner = new StructAssigner(
                structType,
                new StructAssigningAttributes(),
                false);

            return structAssigner.GetValues(actionSet);
        });
    }

    JsonItem ProcessJarray(ParseInfo parseInfo, JArray jarray)
    {
        CodeType arrayOfType = null;
        var arrayItems = new JsonItem[jarray.Count];
        for (int i = 0; i < jarray.Count; i++)
        {
            arrayItems[i] = ProcessJtoken(parseInfo, jarray[i]);

            if (arrayOfType == null)
                arrayOfType = arrayItems[i].type;

            else if (!arrayOfType.Is(arrayItems[i].type))
                arrayOfType = parseInfo.Types.Any();
        }

        if (arrayOfType == null)
            arrayOfType = parseInfo.Types.Any();

        return new JsonItem(new ArrayType(parseInfo.Types, arrayOfType), JsonItemKind.Array, actionSet =>
        {
            return StructHelper.CreateArray(arrayItems.Select(a => a.getValue(actionSet)).ToArray());
        });
    }

    public IWorkshopTree Parse(ActionSet actionSet) => root.getValue(actionSet);

    public CodeType Type() => root.type;

    record struct JsonItem(CodeType type, JsonItemKind kind, Func<ActionSet, IWorkshopTree> getValue);

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