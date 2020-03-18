using Deltin.Deltinteger;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.JSON;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.IO;

class JSONType: CodeType {
    List<InternalVar> Children = new List<InternalVar>();
    public List<InternalVar> Arrays = new List<InternalVar>();

    List<(InternalVar var, JProperty prop)> Properties = new List<(InternalVar var, JProperty prop)>();

    private Scope staticScope = new Scope("JSON");
    private Scope objectScope = new Scope("JSON");


    public JSONType(JObject jsonData) : base("JSON"){
        foreach(JProperty prop in jsonData.Children<JProperty>()){
            switch (prop.Value.Type)
            {
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Null:
                    Properties.Add((CreateInternalVar(prop.Name, "A JSON Property."), prop));
                    break;
                case JTokenType.Array:
                    InternalVar array = CreateInternalVar(prop.Name, "A JSON Array.");
                    array.CodeType = new ArrayType(new JSONArray(prop.Name + "Arr" + Arrays.Count, (JArray)prop.Value));
                    //ArrayType array = new ArrayType(new JSONArray(prop.Name + "Arr" + Arrays.Count, (JArray)prop.Value));
                    Arrays.Add(array);
                    break;

                default:
                    InternalVar child = CreateInternalVar(prop.Name, "A JSON Object.");
                    child.CodeType = new JSONType((JObject)prop.Value);
                    Children.Add(child);
                    break;
            }
        }
    }

    public bool ContainsDeepArrays()
    {
        foreach(InternalVar arr in Arrays)
        {
            if(((JSONArray)((ArrayType)arr.CodeType).ArrayOfType).Arrays.Count > 0)
            {
                return true;
            }
            if (((JSONArray)((ArrayType)arr.CodeType).ArrayOfType).Children.Count > 0)
            {
                return true;
            }
        }
        foreach(InternalVar arr in Children)
        {
            bool containsDeepArrays = ((JSONType)arr.CodeType).ContainsDeepArrays();
            if (containsDeepArrays)
                return true;
        }
        return false;
    }


    private InternalVar CreateInternalVar(string name, string documentation, bool isStatic = false)
    {
        // Create the variable.
        InternalVar newInternalVar = new InternalVar(name, CompletionItemKind.Property);

        // Make the variable unsettable.
        newInternalVar.IsSettable = false;

        // Set the documentation.
        newInternalVar.Documentation = documentation;

        // Add the variable to the object scope.
        if (!isStatic) objectScope.AddNativeVariable(newInternalVar);
        // Add the variable to the static scope.
        else staticScope.AddNativeVariable(newInternalVar);

        return newInternalVar;
    }



    public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner) {
        foreach (var p in Properties) {
            switch (p.prop.Value.Type)
            {
                case JTokenType.String:
                    assigner.Add(p.var, new V_CustomString(p.prop.Value.ToObject<string>()));
                    break;
                case JTokenType.Boolean:
                    bool val = (bool)p.prop.Value;
                    if (val) assigner.Add(p.var, new V_True());
                    else assigner.Add(p.var, new V_False());

                    break;

                case JTokenType.Float:
                    assigner.Add(p.var, new V_Number(p.prop.Value.ToObject<float>()));
                    break;
                case JTokenType.Integer:
                    assigner.Add(p.var, new V_Number(p.prop.Value.ToObject<int>()));
                    break;
                case JTokenType.Null:
                    assigner.Add(p.var, new V_Null());
                    break;
            }
        }
        foreach (var c in Children)
        {
            assigner.Add(c, new V_Null());
        }

        foreach (var a in Arrays)
        {
            assigner.Add(a, ((JSONArray)((ArrayType)a.CodeType).ArrayOfType).ConvertValuesToElementArray());
        }
    }
    public override CompletionItem GetCompletion() => new CompletionItem()
    {
        Label = Name,
        Kind = CompletionItemKind.Struct
    };

    public override Scope GetObjectScope() => objectScope;
        
    public override Scope ReturningScope() => staticScope;
}


//class JSONConstructor : Constructor
//{
//    public JSONConstructor(JSONType jsonType) : base(jsonType, null, AccessLevel.Public)
//    {
//        Parameters = new CodeParameter[] {
//                new JSONFileParameter("jsonFile", "path of the JSON file you want to access. Must be a `.json` file.")
//            };
//        Documentation = Extras.GetMarkupContent("Creates a macro out of a '.json' file.");
//    }

//    public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData) => throw new NotImplementedException();
//}

class JSONFileParameter : FileParameter
{
    public JSONFileParameter(string parameterName, string description) : base(parameterName, description, ".json") { }

    public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
    {
        string filepath = base.Validate(script, value, valueRange) as string;
        if (filepath == null) return null;


        JObject jsonData;

        try
        {
            string jsonText = File.ReadAllText(filepath);;

            jsonData = JObject.Parse(jsonText);
        }
        catch (InvalidOperationException)
        {
            script.Diagnostics.Error("Failed to deserialize the JSON file.", valueRange);
            return null;
        }

        return jsonData;
    }

}