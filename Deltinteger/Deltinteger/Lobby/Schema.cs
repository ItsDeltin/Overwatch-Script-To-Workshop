using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Deltin.Deltinteger.Lobby
{
    public class RootSchema
    {
        [JsonProperty("$schema")]
        public string Schema;

        [JsonProperty("$ref")]
        public string Ref;

        [JsonProperty("$comment")]
        public string Comment;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("definitions")]
        public Dictionary<string, RootSchema> Definitions;

        [JsonProperty("properties")]
        public Dictionary<string, RootSchema> Properties;

        [JsonProperty("default")]
        public object Default;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SchemaObjectType? Type;

        [JsonProperty("minimum")]
        public double Minimum;

        [JsonProperty("maximum")]
        public double Maximum;

        [JsonProperty("enum")]
        public object[] Enum;

        [JsonProperty("uniqueItems")]
        public bool UniqueItems;

        [JsonProperty("items")]
        public RootSchema Items;

        [JsonProperty("oneOf")]
        public RootSchema[] OneOf;

        [JsonProperty("required")]
        public string[] Required;

        [JsonProperty("additionalProperties")]
        public bool? AdditionalProperties;

        public RootSchema() {}

        public RootSchema(string description)
        {
            Description = description;
        }

        public RootSchema InitDefinitions()
        {
            Definitions = new Dictionary<string, RootSchema>();
            return this;
        }

        public RootSchema InitProperties()
        {
            Properties = new Dictionary<string, RootSchema>();
            return this;
        }
    }

    public enum SchemaObjectType
    {
        [EnumMember(Value = "object")]
        Object,
        [EnumMember(Value = "array")]
        Array,
        [EnumMember(Value = "boolean")]
        Boolean,
        [EnumMember(Value = "integer")]
        Integer,
        [EnumMember(Value = "null")]
        Null,
        [EnumMember(Value = "number")]
        Number,
        [EnumMember(Value = "string")]
        String
    }
}