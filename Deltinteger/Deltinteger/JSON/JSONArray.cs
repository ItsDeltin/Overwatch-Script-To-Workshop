using Deltin.Deltinteger;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deltin.JSON
{
    class JSONArray : CodeType
    {
        private Scope staticScope = new Scope("JSONArray");
        private Scope objectScope = new Scope("JSONArray");

        List<(InternalVar Var, JValue Value)> Values = new List<(InternalVar Var, JValue Value)>();
        public List<InternalVar> Children = new List<InternalVar>();
        public List<InternalVar> Arrays = new List<InternalVar>();

        string Name;


        public JSONArray(string name, JArray array) : base("JSONArray")
        {
            Name = name;

            foreach(JToken entry in array)
            {
                switch (entry.Type)
                {
                    case JTokenType.String:
                    case JTokenType.Boolean:
                    case JTokenType.Float:
                    case JTokenType.Integer:
                    case JTokenType.Null:
                        string valName = "Val" + Values.Count;

                        Values.Add((new InternalVar(valName), (JValue)entry));
                        break;
                    case JTokenType.Array:
                        string arrName = "Arr" + Arrays.Count;
                        InternalVar a = CreateInternalVar(arrName, "A JSON Array.");
                        a.CodeType = new ArrayType(new JSONArray(arrName, (JArray)entry));
                        Arrays.Add(a);
                        break;
                    default:

                        string objName = name + "Of" + Children.Count;
                        InternalVar child = CreateInternalVar(objName, "A JSON Object.");
                        child.CodeType = new JSONType((JObject)entry);
                        Children.Add(child);
                        break;
                }
            }
        }



        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            //List<IWorkshopTree> array = new List<IWorkshopTree>();

            foreach (var v in Values)
            {
                switch (v.Value.Type)
                {
                    case JTokenType.String:
                        //array.Add(new V_CustomString(v.Value.ToObject<string>()));
                        assigner.Add(v.Var, new V_CustomString(v.Value.ToObject<string>()));
                        break;

                    case JTokenType.Boolean:
                        bool val = (bool)v.Value;
                        if (val) assigner.Add(v.Var, new V_True());
                        else assigner.Add(v.Var, new V_False());

                        break;

                    case JTokenType.Float:
                        assigner.Add(v.Var, new V_Number(v.Value.ToObject<float>()));
                        break;

                    case JTokenType.Integer:
                        assigner.Add(v.Var, new V_Number(v.Value.ToObject<int>()));
                        break;

                    case JTokenType.Null:
                        assigner.Add(v.Var, new V_Null());
                        break;
                }
            }

            //InternalVar arr = CreateInternalVar(Name, "A JSON Array.");
            //assigner.Add(arr, this.ConvertValuesToElementArray());

            foreach (var c in Children)
            {
                assigner.Add(c, new V_Null());
            }

            foreach (var a in Arrays)
            {
                //((JSONArray)((ArrayType)a.CodeType).ArrayOfType).ConvertValuesToElementArray()
                assigner.Add(a, reference);
            }
        }

        public Element ConvertValuesToElementArray()
        {
            List<IWorkshopTree> array = new List<IWorkshopTree>();

            foreach (var v in Values)
            {
                switch (v.Value.Type)
                {
                    case JTokenType.String:
                        array.Add(new V_CustomString(v.Value.ToObject<string>()));
                        break;

                    case JTokenType.Boolean:
                        bool val = (bool)v.Value;
                        if (val) array.Add(new V_True());
                        else array.Add(new V_False());

                        break;

                    case JTokenType.Float:
                        array.Add(new V_Number(v.Value.ToObject<float>()));
                        break;

                    case JTokenType.Integer:
                        array.Add(new V_Number(v.Value.ToObject<int>()));
                        break;

                    case JTokenType.Null:
                        array.Add(new V_Null());
                        break;
                }
            }
            return Element.CreateArray(array.ToArray());
        }



        private InternalVar CreateInternalVar(string name, string documentation, bool isStatic = false)
        {
            // Create the variable.
            InternalVar newInternalVar = new InternalVar(name, CompletionItemKind.Constant);

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


        public override CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };


        public override Scope GetObjectScope() => objectScope;

        public override Scope ReturningScope() => staticScope;
    }
}
