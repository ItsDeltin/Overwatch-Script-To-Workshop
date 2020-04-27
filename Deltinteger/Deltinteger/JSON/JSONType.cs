using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;
using Newtonsoft.Json.Linq;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Json
{
    class JsonType : CodeType
    {
        public List<JsonProperty> Properties { get; } = new List<JsonProperty>();
        private Scope objectScope = new Scope("JSON");

        public JsonType(JObject jsonData) : base("JSON")
        {
            objectScope.AddNativeMethod(new GetJsonPropertyFunction(this));

            foreach(JProperty prop in jsonData.Children<JProperty>())
            {
                JsonProperty newProperty = new JsonProperty(prop);
                Properties.Add(newProperty);
                objectScope.AddNativeVariable(newProperty.Var);
            }
        }

        public bool ContainsDeepArrays()
        {
            foreach (JsonProperty property in Properties)
                if (property.Value.ContainsDeepArrays())
                    return true;
            return false;
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            foreach (var p in Properties)
            {
                if(p.Value.Value != null)
                {
                    assigner.Add(p.Var, p.Value.Value);
                } else
                {
                    assigner.Add(p.Var, new V_Null());
                }
            }
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => throw new NotImplementedException();
        public override Scope GetObjectScope() => objectScope;

        public static Element ElementFromProperty(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.String: return new V_CustomString(value.ToObject<string>());
                case JTokenType.Boolean: return value.ToObject<bool>() ? (Element)new V_True() : new V_False();
                case JTokenType.Float:
                case JTokenType.Integer: return new V_Number(value.ToObject<double>());
                default:
                case JTokenType.Null: return new V_Null();
            }
        }
    }

    class JsonProperty
    {
        public string Name { get; }
        public InternalVar Var { get; }
        public IJsonValue Value { get; }

        public JsonProperty(JProperty property)
        {
            Name = property.Name;
            Var = new JsonVar(property.Name);
            Var.IsSettable = false;
            Value = IJsonValue.GetValue(property.Value);
            Var.Documentation = Value.Documentation;
            Var.CodeType = Value.Type;
        }
    }

    interface IJsonValue
    {
        Element Value { get; }
        string Documentation { get; }
        CodeType Type { get; }
        bool ContainsDeepArrays() => false;

        public static IJsonValue GetValue(JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Null: return new JsonValue(value);
                case JTokenType.Array: return new JsonArray(value);
                default: return new JsonObject(value);
            }
        }
    }

    class JsonValue : IJsonValue
    {
        public Element Value { get; }
        public string Documentation { get; }
        public CodeType Type => null;

        public JsonValue(JToken token)
        {
            Value = JsonType.ElementFromProperty(token);

            string codeDescription;
            string additionalDescription = "A json " + token.Type.ToString().ToLower() + ".";
            switch (token.Type)
            {
                case JTokenType.String:
                {
                    // Get the string value.
                    string str = token.ToObject<string>();
                    codeDescription = "\"" + str + "\"";
                    break;
                }
                case JTokenType.Boolean:
                {
                    bool val = token.ToObject<bool>();
                    codeDescription = val.ToString().ToLower();
                    break;
                }
                
                case JTokenType.Float:
                case JTokenType.Integer:
                {
                    double val = token.ToObject<double>();
                    codeDescription = val.ToString();
                    break;
                }
                
                case JTokenType.Null:
                    codeDescription = "null";
                    break;
                
                default: throw new NotImplementedException();
            }
            
            Documentation = new MarkupBuilder().StartCodeLine()
                .Add(codeDescription)
                .EndCodeLine()
                .NewSection()
                .Add(additionalDescription)
                .ToString();
        }
    }

    class JsonArray : IJsonValue
    {
        public Element Value { get; }
        public IJsonValue[] Children { get; }
        public string Documentation { get; }
        public CodeType Type => null;

        public JsonArray(JToken token)
        {
            Documentation = "A json array.";
            JArray array = (JArray)token;

            Children = new IJsonValue[array.Count];
            for (int i = 0; i < Children.Length; i++)
                Children[i] = IJsonValue.GetValue(array[i]);
            
            Value = Element.CreateArray(Children.Select(c => c.Value).ToArray());
        }

        public bool ContainsDeepArrays() => Children.Any(c => c is JsonArray);
    }

    class JsonObject : IJsonValue
    {
        public Element Value => null;
        public string Documentation { get; }
        public CodeType Type { get; }

        public JsonVar Var { get; }

        public JsonObject(JToken token)
        {
            Documentation =  "A JSON object.";
            Type = new JsonType((JObject)token);
        }

        public bool ContainsDeepArrays() => ((JsonType)Type).Properties.Any(p => p.Value.ContainsDeepArrays());
    }

    class JsonVar : InternalVar
    {
        public JsonVar(string name) : base(name, CompletionItemKind.Property) {}
        public override string GetLabel(bool markdown)
        {
            if (!markdown) return base.GetLabel(false);
            return Documentation;
        }
    }

    class GetJsonPropertyFunction : IMethod
    {
        public MethodAttributes Attributes { get; }
        public CodeParameter[] Parameters { get; }
        public string Name => "Get";
        public CodeType ReturnType => null;
        public bool Static => false;
        public bool WholeContext => true;
        public string Documentation => "Gets a property value from a string. Used for getting properties whos name cannot be typed in code.";
        public Deltin.Deltinteger.LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public bool DoesReturnValue() => true;
        private JsonType ContainingType { get; }

        public GetJsonPropertyFunction(JsonType containingType)
        {
            Attributes = new MethodAttributes() {
                ContainingType = containingType
            };
            Parameters = new CodeParameter[] {
                new GetPropertyParameter(containingType)
            };
            ContainingType = containingType;
        }

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Detail = GetLabel(false),
            Kind = CompletionItemKind.Method,
            Documentation = Documentation
        };

        public string GetLabel(bool markdown) => HoverHandler.GetLabel("define", Name, Parameters, markdown, Documentation);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall) => (Element)methodCall.AdditionalParameterData[0];

        class GetPropertyParameter : CodeParameter
        {
            private JsonType containingType { get; }

            public GetPropertyParameter(JsonType type) : base("propertyName", type:null)
            {
                containingType = type;
            }

            public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;

            public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
            {
                StringAction stringAction = value as StringAction;
                if (stringAction == null)
                {
                    script.Diagnostics.Error("Expected string constant.", valueRange);
                    return null;
                }

                List<CompletionItem> completion = new List<CompletionItem>();
                foreach (var prop in containingType.Properties)
                    completion.Add(new CompletionItem() {
                        Label = prop.Name,
                        Detail = prop.Name,
                        Documentation = Extras.GetMarkupContent(prop.Var.Documentation),
                        Kind = CompletionItemKind.Property
                    });
                script.AddCompletionRange(new CompletionRange(completion.ToArray(), valueRange, CompletionRangeKind.ClearRest));

                string text = stringAction.Value;

                // Check properties.
                foreach (var prop in containingType.Properties)
                    if (prop.Name == text)
                        return prop.Value.Value;
                
                script.Diagnostics.Error($"Could not find the property '{text}'.", valueRange);
                return null;
            }
        }
    }
}