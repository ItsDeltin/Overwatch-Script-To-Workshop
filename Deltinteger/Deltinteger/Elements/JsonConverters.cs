using System;
using System.Linq;
using System.Collections.Generic;
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

                ElementEnum newEnum = new ElementEnum();
                newEnum.Name = name;

                // Direct
                if (reader.TokenType == JsonToken.StartArray)
                {
                    newEnum.Members = GetEnumMembers(newEnum, JToken.Load(reader).ToArray());
                }
                // With additional properties
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // Get the enum object.
                    var enumObject = JObject.Load(reader);

                    // 'hidden' property
                    if (enumObject.TryGetValue("hidden", out JToken hiddenValue))
                        newEnum.Hidden = hiddenValue.ToObject<bool>();
                    
                    // Members
                    newEnum.Members = GetEnumMembers(newEnum, enumObject.GetValue("members").ToArray());
                }

                // Advance past EndObject or EndArray
                reader.Read();

                enumerators.Add(newEnum);
            }

            return enumerators.ToArray();
        }

        private ElementEnumMember[] GetEnumMembers(ElementEnum newEnum, JToken[] members) => members.Select(v => {
            // String
            if (v.Type == JTokenType.String)
            {
                string name = v.ToObject<string>();
                return new ElementEnumMember() {
                    Name = name,
                    I18n = name,
                    Enum = newEnum
                };
            }
            // Object
            else if (v.Type == JTokenType.Object)
            {
                string name = v["name"].ToObject<string>();
                return new ElementEnumMember() {
                    Name = name,
                    Alias = v["alias"]?.ToObject<string>(),
                    I18n = v["i18n"]?.ToObject<string>() ?? name,
                    Enum = newEnum
                };
            }
            // Unknown
            throw new NotImplementedException(v.Type.ToString());
        }).ToArray();

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
                var parameterObject = JObject.Load(reader);
                var parameter = parameterObject.ToObject<ElementParameter>();

                // Get the name.
                parameter.Name = name;

                // Get the default value state.
                parameter.HasDefaultValue = parameterObject.ContainsKey("defaultValue");

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
}