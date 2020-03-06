using System;
using System.Collections.Generic;
using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.I18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby
{
    public class Ruleset
    {
        public static readonly LobbySetting[] LobbySettings = new LobbySetting[] {
            new SelectValue("Map Rotation", "After A Mirror Match", "After A Game", "Paused"),
            new SelectValue("Return To Lobby", "After A Mirror Match", "After A Game", "Never"),
            new SelectValue("Team Balancing", "Off", "After A Mirror Match", "After A Game"),
            new SwitchValue("Swap Teams After Match", true),
            new RangeValue("Max Team 1 Players", 0, 6, 6),
            new RangeValue("Max Team 2 Players", 0, 6, 6),
            new RangeValue("Max FFA Players", 0, 12, 0),
            new RangeValue("Max Spectators", 0, 12, 2),
            new SwitchValue("Allow Players Who Are In Queue", false),
            new SwitchValue("Use Experimental Update If Available", false),
            new SwitchValue("Match Voice Chat", false),
            new SwitchValue("Pause Game On Player Disconnect", false)
        };

        public WorkshopValuePair Lobby { get; set; }
        public ModesRoot Modes { get; set; }
        public HeroesRoot Heroes { get; set; }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            builder.AppendKeywordLine("settings");
            builder.AppendLine("{");
            builder.Indent();

            // Get the lobby settings.
            if (Lobby != null)
            {
                builder.AppendKeywordLine("lobby");
                builder.AppendLine("{");
                builder.Indent();
                Lobby.ToWorkshop(builder);
                builder.Unindent();
                builder.AppendLine("}");
            }
            
            // Get the mode settings.
            if (Modes != null) Modes.ToWorkshop(builder);

            // Get the hero settings.
            if (Heroes != null) Heroes.ToWorkshop(builder);

            builder.Unindent();
            builder.AppendLine("}");
        }

        public static Ruleset Parse(JObject json)
        {
            Ruleset result = json.ToObject<Ruleset>();
            result.Modes?.MergeModeSettings();
            return result;
        }

        public static void GenerateSchema()
        {
            // Initialize the root.
            RootSchema root = new RootSchema().InitDefinitions().InitProperties();
            root.Schema = "http://json-schema.org/draft-04/schema#";
            root.Type = SchemaObjectType.Object;
            root.Title = "JSON schema for OSTW lobby setting files.";

            SchemaGenerate generate = new SchemaGenerate(root.Definitions);

            root.Properties.Add("Lobby", GetLobby(generate));
            root.Properties.Add("Modes", GetModes(generate));
            root.Definitions.Add("HeroList", GetHeroList(generate));

            // Get the hero settings.
            RootSchema heroesRoot = new RootSchema("Hero settings.").InitProperties();
            root.Properties.Add("Heroes", heroesRoot);

            heroesRoot.Properties.Add("General", GetHeroListReference("The list of hero settings that affects both teams."));
            heroesRoot.Properties.Add("Team 1", GetHeroListReference("The list of hero settings that affects team 1."));
            heroesRoot.Properties.Add("Team 2", GetHeroListReference("The list of hero settings that affects team 2."));
            
            // Get the result.
            string result = JsonConvert.SerializeObject(root, new JsonSerializerSettings() {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented
            });

            Program.WorkshopCodeResult(result);
        }

        private static RootSchema GetHeroListReference(string description)
        {
            RootSchema schema = new RootSchema(description);
            schema.Ref = "#/definitions/HeroList";
            return schema;
        }

        private static RootSchema GetHeroList(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema().InitProperties();
            foreach (var heroSettings in HeroSettingCollection.AllHeroSettings) schema.Properties.Add(heroSettings.HeroName, heroSettings.GetSchema(generate));
            return schema;
        }

        private static RootSchema GetLobby(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema().InitProperties();
            foreach (var lobbySetting in LobbySettings) schema.Properties.Add(lobbySetting.Name, lobbySetting.GetSchema(generate));
            return schema;
        }

        private static RootSchema GetModes(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema().InitProperties();
            foreach (var mode in ModeSettingCollection.AllModeSettings) schema.Properties.Add(mode.ModeName, mode.GetSchema(generate));
            return schema;
        }
    
        /// <summary>Gets the keywords used for translation.</summary>
        public static string[] Keywords()
        {
            List<string> keywords = new List<string>();

            keywords.Add("settings");
            keywords.Add("lobby");
            keywords.Add("modes");
            keywords.Add("heroes");

            // Get hero keywords.
            foreach (var heroCollection in HeroSettingCollection.AllHeroSettings)
                keywords.AddRange(heroCollection.GetKeywords());
            
            // Get mode keywords.
            foreach (var modeCollection in ModeSettingCollection.AllModeSettings)
            {
                keywords.Add(modeCollection.ModeName);
                keywords.AddRange(modeCollection.GetKeywords());
            }

            return keywords.ToArray();
        }
    }

    public class SchemaGenerate
    {
        public Dictionary<string, RootSchema> Definitions { get; }

        public SchemaGenerate(Dictionary<string, RootSchema> definitions)
        {
            Definitions = definitions;
        }
    }

    public class HeroesRoot
    {
        [JsonProperty("General")]
        public HeroList General { get; set; }

        [JsonProperty("Team 1")]
        public HeroList Team1 { get; set; }

        [JsonProperty("Team 2")]
        public HeroList Team2 { get; set; }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            builder.AppendKeywordLine("heroes");
            builder.AppendLine("{");
            builder.Indent();

            General?.ToWorkshop(builder);
            Team1  ?.ToWorkshop(builder);
            Team2  ?.ToWorkshop(builder);

            builder.Unindent();
            builder.AppendLine("}");
        }
    }

    public class WorkshopValuePair : Dictionary<String, object>
    {
        public void ToWorkshop(WorkshopBuilder builder)
        {
            foreach (var setting in this)
            {
                string name = SettingNameResolver.ResolveName(builder, setting.Key);
                string value;

                if (setting.Value is string asString) value = builder.Translate(asString);
                else if (setting.Value is bool asBool) value = builder.Translate(asBool ? "True" : "False");
                else value = setting.Value.ToString();

                builder.AppendLine($"{name}: {value}");
            }
        }
    }

    public class HeroList : Dictionary<String, WorkshopValuePair>
    {
        public void ToWorkshop(WorkshopBuilder builder)
        {
            foreach (var hero in this)
            {
                builder.AppendLine($"{hero.Key}");
                builder.AppendLine("{");
                builder.Indent();
                hero.Value.ToWorkshop(builder);
                builder.Unindent();
                builder.AppendLine("}");
            }
        }
    }

    public abstract class LobbySettingCollection<T> : List<LobbySetting>
    {
        protected string Title;

        public new T Add(LobbySetting lobbySetting)
        {
            base.Add(lobbySetting);
            return (T)(object)this;
        }

        public T AddRange(string name, double min = 0, double max = 500, double defaultValue = 100)
        {
            Add(new RangeValue(name, min, max, defaultValue));
            return (T)(object)this;
        }

        public T AddIntRange(string name, int min, int max, int defaultValue, string referenceName = null)
        {
            Add(new RangeValue(true, name, min, max, defaultValue) { ReferenceName = referenceName ?? name });
            return (T)(object)this;
        }

        public T AddSwitch(string name, bool defaultValue)
        {
            Add(new SwitchValue(name, defaultValue));
            return (T)(object)this;
        }

        public T AddSelectRef(string referenceName, string name, params string[] values)
        {
            Add(new SelectValue(name, values) { ReferenceName = referenceName });
            return (T)(object)this;
        }

        public T AddSelect(string name, params string[] values)
        {
            Add(new SelectValue(name, values));
            return (T)(object)this;
        }

        public RootSchema GetSchema(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema(Title).InitProperties();
            foreach (var value in this) schema.Properties.Add(value.Name, value.GetSchema(generate));
            return schema;
        }

        public string[] GetKeywords()
        {
            List<string> keywords = new List<string>();

            keywords.Add(AbilityNameResolver.CooldownTime);
            keywords.Add(AbilityNameResolver.RechargeRate);
            keywords.Add(AbilityNameResolver.MaximumTime);
            keywords.Add(AbilityNameResolver.UltimateAbility);
            keywords.Add(AbilityNameResolver.UltimateGeneration);
            keywords.Add(AbilityNameResolver.UltimateGenerationCombat);
            keywords.Add(AbilityNameResolver.UltimateGenerationPassive);

            foreach (LobbySetting setting in this)
            {
                keywords.AddRange(SettingNameResolver.Keywords(setting.Name));
                keywords.AddRange(setting.AdditionalKeywords());
            }

            return keywords.ToArray();
        }
    }
}