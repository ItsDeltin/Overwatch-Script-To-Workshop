using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Decompiler.Json
{
    public class DecompilerMeta
    {
        public const string MetaRuleName = "Ostw: Decompiler Meta";

        [JsonProperty("extGlobal")]
        public string[] ExtendedGlobalVariables;
        [JsonProperty("extPlayer")]
        public string[] ExtendedPlayerVariables;

        public static Rule Generate(DeltinScript deltinScript)
        {
            DecompilerMeta obj = new DecompilerMeta()
            {
                ExtendedGlobalVariables = deltinScript.VarCollection.ExtendedVariableList(true).Select(v => v.DebugName).ToArray(),
                ExtendedPlayerVariables = deltinScript.VarCollection.ExtendedVariableList(false).Select(v => v.DebugName).ToArray()
            };

            string json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            int start = 0;

            // Split the json into strings that are 256 or less bytes.
            List<string> split = new List<string>();
            for (int i = 0; i < json.Length; i++)
                if (i == json.Length - 1 || Encoding.UTF8.GetByteCount(json, start, i + 1 - start) > 256)
                {
                    split.Add(json.Substring(start, i + 1 - start));
                    start = i;
                }

            // Create the actions.
            List<Element> actions = new List<Element>();
            for (int i = 0; i < split.Count; i++)
            {
                Element element;
                if (i < split.Count - 1) element = Element.Part("Continue");
                else element = Element.Part("End");

                element.Disabled = true; // Disable the action.
                element.Comment = split[i] // Set the comment.
                    .Replace("\"", "\\\"") // Escape quotes
                    ;

                actions.Add(element);
            }

            // Create and return the generated rule.
            return new Rule(MetaRuleName) { Disabled = true, Priority = -1, Actions = actions.ToArray() };
        }

        public static DecompilerMeta Load(TTERule rule)
        {
            StringBuilder json = new StringBuilder();
            foreach (var action in rule.Actions) json.Append(action.Comment);

            // Unescape quotes
            json.Replace("\\\"", "\"");

            return JsonConvert.DeserializeObject<DecompilerMeta>(json.ToString());
        }
    }
}