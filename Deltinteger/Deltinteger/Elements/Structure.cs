using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Elements
{
    public class EnumeratorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ElementEnum[]) == objectType;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumerator = existingValue as ElementEnum[] ?? new ElementEnum[0];

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) continue;

                var value = reader.Value.ToString();

            }

            return enumerator;
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
            var parameter = existingValue as ElementParameter[] ?? new ElementParameter[0];

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) continue;
                var value = reader.Value.ToString();
            }

            return parameter;
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
            => JsonConvert.DeserializeObject<ElementJsonRoot>(json, new EnumeratorConverter());
        
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
                    string defaultValue = null; // Initialize the default value.

                    // Get the type name from a parameter.
                    if (el.WorkshopParameters[i] is Parameter parameter)
                    {
                        returnType = GetTypeName(parameter.ReturnType);
                        // Get the default value.
                        if (parameter.GetDefault() is Element defaultElement) defaultValue = defaultElement.Name;
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
                        ReturnType = GetTypeName(el.ElementValueType)
                    });
                }
                else
                {
                    // Add action
                    actions.Add(new ElementJsonAction() {
                        Name = el.WorkshopName,
                        Documentation = el.Documentation,
                        Parameters = parameters
                    });
                }
            }
            
            ElementEnum[] enumerators = EnumData.GetEnumData().Select(e => new ElementEnum() {
                Name = e.CodeName,
                Members = e.Members.Select(mem => new ElementEnumMember() {
                    Name = mem.WorkshopName
                }).ToArray()
            }).ToArray();
            
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
    }

    public class ElementEnum
    {
        public string Name;
        public ElementEnumMember[] Members;
    }

    public class ElementEnumMember
    {
        public string Name;
        public string Alias;
    }
}