using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Deltin.Deltinteger.Elements
{
    public class EnumeratorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ElementEnum[]) == objectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumerators = new List<ElementEnum>();

            // When ReadJson is called, reader's token type is StartObject.
            reader.Read(); // Advance to property name.
            
            while (reader.TokenType != JsonToken.EndObject)
            {
                string name = (string)reader.Value;
                reader.Read(); // Advance to the next object.

                // Direct
                if (reader.TokenType == JsonToken.StartArray)
                {
                    enumerators.Add(new ElementEnum() {
                        Name = name,
                        Members = JToken.Load(reader).ToArray().Select(v => {
                            // String
                            if (v.Type == JTokenType.String)
                                return new ElementEnumMember() {
                                    Name = v.ToObject<string>()
                                };
                            // Object
                            else if (v.Type == JTokenType.Object)
                                return new ElementEnumMember() {
                                    Name = v["name"].ToObject<string>(),
                                    Alias = v["alias"].ToObject<string>()
                                };
                            // Unknown
                            throw new NotImplementedException(v.Type.ToString());
                        }).ToArray()
                    });
                }
                // With additional properties
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // todo
                }
            }

            return enumerators.ToArray();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var enumerator in (ElementEnum[])value)
            {
                // Write the enum name.
                writer.WritePropertyName(enumerator.Name);

                // Member array.
                writer.WriteStartArray();

                // Write the members.
                foreach (var member in enumerator.Members)
                {
                    // No additional properties, just write the name.
                    if (member.Alias == null)
                        writer.WriteValue(member.Name);
                    else
                    {
                        // Start the object.
                        writer.WriteStartObject();

                        // Write the name.
                        writer.WritePropertyName("name");
                        writer.WriteValue(member.Name);

                        // Write the alias.
                        writer.WritePropertyName("alias");
                        writer.WriteValue(member.Alias);

                        // End the object.
                        writer.WriteEndObject();
                    }
                }

                // End the member array.
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }

    public class ParameterConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ElementParameter[]) == objectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // var parameter = existingValue as ElementParameter[] ?? new ElementParameter[0];
            List<ElementParameter> parameters = new List<ElementParameter>();

            // When ReadJson is called, reader's token type is StartObject.
            reader.Read(); // Advance to property name.

            while (reader.TokenType != JsonToken.EndObject)
            {
                string name = (string)reader.Value;
                reader.Read(); // Advance to the next object.

                // Convert the parameter to an object.
                var parameter = JObject.Load(reader).ToObject<ElementParameter>();

                // Get the name.
                parameter.Name = name;

                // Add it to the parameter list.
                parameters.Add(parameter);

                // Advance
                reader.Read();
            }

            return parameters.ToArray();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var parameter in (ElementParameter[])value)
            {
                 // Write the property name.
                writer.WritePropertyName(parameter.Name);

                // Serialize the actual value.
                serializer.Serialize(writer, parameter);
            }

            writer.WriteEndObject();
        }
    }

    public class ElementJsonRoot
    {
        [JsonProperty("values")]
        public ElementJsonValue[] Values;

        [JsonProperty("actions")]
        public ElementJsonAction[] Actions;

        [JsonProperty("enumerators")]
        public ElementEnum[] Enumerators;

        public static ElementJsonRoot Get(string json)
            => JsonConvert.DeserializeObject<ElementJsonRoot>(json, new EnumeratorConverter(), new ParameterConverter());
        
        public static string Make()
        {
            var values = new List<ElementJsonValue>();
            var actions = new List<ElementJsonAction>();
            
            foreach (var el in ElementList.Elements)
            {
                // Get the parameters.                
                ElementParameter[] parameters = new ElementParameter[el.WorkshopParameters.Length];
                for (int i = 0; i < el.WorkshopParameters.Length; i++)
                {
                    // Initialize the variables that will be used to create the ElementParameter.
                    string returnType = null; // Initialize the return type's literal name.
                    bool? variableReferenceGlobal = null; // Initialize the variable reference type.
                    object defaultValue = null; // Initialize the default value.

                    // Get the type name from a parameter.
                    if (el.WorkshopParameters[i] is Parameter parameter)
                    {
                        returnType = GetTypeName(parameter.ReturnType);
                        // Get the default value.
                        if (parameter.GetDefault() is Element defaultElement)
                        {
                            // Numbers
                            if (defaultElement is V_Number numberElement)
                                defaultValue = numberElement.Value;
                            // Strings
                            else if (defaultElement is V_CustomString customString)
                                defaultValue = "!" + customString.Text;
                            // Localized
                            else if (defaultElement is V_String str)
                                defaultValue = "@" + str.Text;
                            // True
                            else if (defaultElement is V_True)
                                defaultValue = true;
                            // False
                            else if (defaultElement is V_False)
                                defaultValue = false;
                            // TODO will not convert / Null
                            else if (defaultElement is V_Null)
                                defaultValue = null;
                            // Other
                            else
                                defaultValue = defaultElement.Name;
                        }
                    }
                    // Get the type name from an enum.
                    else if (el.WorkshopParameters[i] is EnumParameter enumParameter)
                    {
                        returnType = enumParameter.EnumData.CodeName;
                        // Get the default value.
                        defaultValue = enumParameter.EnumData.Members[0].WorkshopName;
                    }
                    // Get the expected variable type.
                    else if (el.WorkshopParameters[i] is VarRefParameter varRef)
                        variableReferenceGlobal = varRef.IsGlobal;

                    // Set the parameter with the retrieved data.
                    parameters[i] = new ElementParameter() {
                        Name = el.WorkshopParameters[i].Name,
                        Documentation = el.Parameters[i].Documentation,
                        Type = returnType,
                        VariableReferenceIsGlobal = variableReferenceGlobal,
                        DefaultValue = defaultValue
                    };
                }
                if (parameters.Length == 0) parameters = null;

                // Add the element.
                if (el.IsValue)
                {
                    // Add value
                    values.Add(new ElementJsonValue() {
                        Name = el.WorkshopName,
                        Documentation = el.Documentation,
                        Parameters = parameters,
                        IsHidden = el.Hidden,
                        ReturnType = GetTypeName(el.ElementValueType)
                    });
                }
                else
                {
                    // Add action
                    actions.Add(new ElementJsonAction() {
                        Name = el.WorkshopName,
                        Documentation = el.Documentation,
                        IsHidden = el.Hidden,
                        Parameters = parameters
                    });
                }
            }
            
            // Get the enumerators.
            ElementEnum[] enumerators = EnumData.GetEnumData().Select(e => new ElementEnum() {
                Name = e.CodeName,
                Members = e.Members.Select(mem => new ElementEnumMember() {
                    Name = mem.WorkshopName
                }).ToArray()
            }).ToArray();
            
            // Create the object then get the json.
            return new ElementJsonRoot() {
                Actions = actions.ToArray(),
                Values = values.ToArray(),
                Enumerators = enumerators
            }.ToJson();
        }

        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings() {
                // Set converters
                Converters = new JsonConverter[] {
                    new EnumeratorConverter(),
                    new ParameterConverter()
                },
                // Ignore null
                NullValueHandling = NullValueHandling.Ignore
            });

        private static string GetTypeName(ValueType valueType)
        {
            switch (valueType)
            {
                case ValueType.Any: return "any";
                case ValueType.Boolean: return "boolean";
                case ValueType.Vector: return "vector";
                case ValueType.VectorAndPlayer: return "player | vector";
                case ValueType.Button: return "button";
                case ValueType.Gamemode: return "gamemode";
                case ValueType.Hero: return "hero";
                case ValueType.Map: return "map";
                case ValueType.Number: return "number";
                case ValueType.Player: return "player";
                case ValueType.String: return "string";
                case ValueType.Team: return "team";
                default: throw new NotImplementedException(valueType.ToString());
            }
        }
    }

    public class ElementBaseJson
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("documentation")]
        public string Documentation;

        [JsonProperty("parameters")]
        public ElementParameter[] Parameters;

        [JsonProperty("hidden")]
        public bool IsHidden;

        public bool ShouldSerializeIsHidden() => IsHidden;

        public override string ToString() => Name + (Parameters == null ? "" : "(" + string.Join(", ", Parameters.Select(v => v.ToString())) + ")");
    }

    public class ElementJsonValue : ElementBaseJson
    {
        [JsonProperty("type")]
        public string ReturnType;
    }

    public class ElementJsonAction : ElementBaseJson
    {
        [JsonProperty("return-value")]
        public string ReturnValue;
    }

    public class ElementParameter
    {
        [JsonIgnore]
        public string Name;

        [JsonProperty("documentation")]
        public string Documentation;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("defaultValue")]
        public object DefaultValue;

        [JsonProperty("var-ref-global")]
        public bool? VariableReferenceIsGlobal;

        public override string ToString() => Type + " " + Name;
    }

    public class ElementEnum
    {
        public string Name;
        public ElementEnumMember[] Members;

        public override string ToString() => Name + " [" + Members.Length + " members]";
    }

    public class ElementEnumMember
    {
        public string Name;
        public string Alias;
        public override string ToString() => Name;
    }
}