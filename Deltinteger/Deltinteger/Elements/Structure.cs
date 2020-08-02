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
                    ElementEnum newEnum = new ElementEnum();
                    newEnum.Name = name;
                    newEnum.Members = JToken.Load(reader).ToArray().Select(v => {
                            // String
                            if (v.Type == JTokenType.String)
                                return new ElementEnumMember() {
                                    Name = v.ToObject<string>(),
                                    Enum = newEnum
                                };
                            // Object
                            else if (v.Type == JTokenType.Object)
                                return new ElementEnumMember() {
                                    Name = v["name"].ToObject<string>(),
                                    Alias = v["alias"].ToObject<string>(),
                                    Enum = newEnum
                                };
                            // Unknown
                            throw new NotImplementedException(v.Type.ToString());
                        }).ToArray();

                    enumerators.Add(newEnum);
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

    public class ElementRoot
    {
        public static ElementRoot Instance { get; }

        [JsonProperty("values")]
        public ElementJsonValue[] Values;

        [JsonProperty("actions")]
        public ElementJsonAction[] Actions;

        [JsonProperty("enumerators")]
        public ElementEnum[] Enumerators;

        public ElementBaseJson GetFunction(string name)
        {
            foreach (var value in Values) if (value.Name == name) return value;
            foreach (var action in Actions) if (action.Name == name) return action;
            throw new KeyNotFoundException("The function '" + name + "' was not found.");
        }

        public ElementEnum GetEnum(string name) => Enumerators.FirstOrDefault(e => e.Name == name) ?? throw new KeyNotFoundException("The enum '" + name + "' was not found.");

        public static ElementRoot Get(string json)
            => JsonConvert.DeserializeObject<ElementRoot>(json, new EnumeratorConverter(), new ParameterConverter());
        
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

    public abstract class ElementBaseJson
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("documentation")]
        public string Documentation;

        [JsonProperty("parameters")]
        public ElementParameter[] Parameters;

        [JsonProperty("hidden")]
        public bool IsHidden;

        [JsonProperty("alias")]
        public string Alias;

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

        public ElementEnumMember GetMemberFromAlias(string name) => Members.FirstOrDefault(m => (m.Alias ?? m.Name) == name) ?? throw new KeyNotFoundException("The enum member '" + name + "' was not found.");
    }

    public class ElementEnumMember : IWorkshopTree
    {
        public string Name;
        public string Alias;
        public ElementEnum Enum;

        public override string ToString() => Name;

        public string ToWorkshop(OutputLanguage language, ToWorkshopContext context) => Name;

        public bool EqualTo(IWorkshopTree other)
        {
            throw new NotImplementedException();
        }
    }
}