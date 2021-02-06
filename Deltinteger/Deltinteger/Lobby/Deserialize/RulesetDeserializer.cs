using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby.Deserializer
{
    class RulesetDeserializer : JsonConverter
    {
        public static Ruleset Deserialize(JObject obj)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new RulesetDeserializer());
            return obj.ToObject<Ruleset>(serializer);
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Ruleset);

        // ********
        // * Read *
        // ********
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var rulesetReader = new RulesetReader();

            var root = JObject.Load(reader);
            foreach (var child in root.Children())
                rulesetReader.ReadRootProp((JProperty)child);
            
            return rulesetReader.GetRuleset();
        }

        // *********
        // * Write *
        // *********
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}