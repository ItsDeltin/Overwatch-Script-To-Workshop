using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
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
            new SwitchValue("Swap Teams After Match", true, SwitchType.YesNo),
            new RangeValue(true, "Max Team 1 Players", 0, 12, 6),
            new RangeValue(true, "Max Team 2 Players", 0, 12, 6),
            new RangeValue(true, "Max FFA Players", 0, 12, 0),
            new RangeValue(true, "Max Spectators", 0, 12, 2),
            new SwitchValue("Allow Players Who Are In Queue", false, SwitchType.YesNo),
            new SwitchValue("Use Experimental Update If Available", false, SwitchType.YesNo),
            new SwitchValue("Match Voice Chat", false, SwitchType.EnabledDisabled),
            new SwitchValue("Pause Game On Player Disconnect", false, SwitchType.YesNo),
            new SelectValue("Data Center Preference", "Best Available", "USA - Central", "Brazil", "Singapore", "USA - West", "Australia 3")
        };

        public WorkshopValuePair Lobby { get; set; }
        public ModesRoot Modes { get; set; }
        public HeroesRoot Heroes { get; set; }
        public string Description { get; set; }
        public WorkshopValuePair Workshop { get; set; }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            List<LobbySetting> allSettings = GetAllSettings();

            builder.AppendKeywordLine("settings");
            builder.AppendLine("{");
            builder.Indent();

            // Get the description
            if (Description != null)
            {
                builder.AppendKeywordLine("main")
                    .AppendLine("{")
                    .Indent()
                    .AppendKeyword("Description").Append(": \"" + Description + "\"").AppendLine()
                    .Outdent()
                    .AppendLine("}");
            }

            // Get the lobby settings.
            if (Lobby != null)
            {
                builder.AppendKeywordLine("lobby");
                builder.AppendLine("{");
                builder.Indent();
                Lobby.ToWorkshop(builder, allSettings);
                builder.Outdent();
                builder.AppendLine("}");
            }
            
            // Get the mode settings.
            if (Modes != null) Modes.ToWorkshop(builder, allSettings);

            // Get the hero settings.
            if (Heroes != null) Heroes.ToWorkshop(builder, allSettings);

            // Get the custom workshop settings.
            if (Workshop != null)
            {
                builder.AppendKeywordLine("workshop");
                builder.AppendLine("{");
                builder.Indent();
                Workshop.ToWorkshopCustom(builder);
                builder.Outdent();
                builder.AppendLine("}");
            }

            builder.Outdent();
            builder.AppendLine("}");
        }

        public static List<LobbySetting> GetAllSettings()
        {
            List<LobbySetting> settings = new List<LobbySetting>();
            settings.AddRange(LobbySettings);

            foreach (var heroSettings in HeroSettingCollection.AllHeroSettings) settings.AddRange(heroSettings);
            foreach (var modeSettings in ModeSettingCollection.AllModeSettings) settings.AddRange(modeSettings);

            return settings;
        }

        public static Ruleset Parse(JObject json)
        {
            Ruleset result = json.ToObject<Ruleset>();
            //result.Modes?.MergeModeSettings();
            return result;
        }

        public static void GenerateSchema()
        {
            // Initialize the root.
            RootSchema root = new RootSchema().InitDefinitions().InitProperties();
            root.Schema = "http://json-schema.org/draft-04/schema#";
            root.Type = SchemaObjectType.Object;
            root.Title = "JSON schema for OSTW lobby setting files.";
            root.AdditionalProperties = false;

            SchemaGenerate generate = new SchemaGenerate(root.Definitions);

            root.Properties.Add("Lobby", GetLobby(generate)); // Add 'Lobby' property.
            root.Properties.Add("Modes", GetModes(generate)); // Add 'Modes' property.
            root.Definitions.Add("HeroList", GetHeroList(generate));

            // Get the hero settings.
            RootSchema heroesRoot = new RootSchema("Hero settings.").InitProperties();
            root.Properties.Add("Heroes", heroesRoot); // Add 'Heroes' property.

            // Add team properties to heroes.
            heroesRoot.Properties.Add("General", GetHeroListReference("The list of hero settings that affects both teams."));
            heroesRoot.Properties.Add("Team 1", GetHeroListReference("The list of hero settings that affects team 1."));
            heroesRoot.Properties.Add("Team 2", GetHeroListReference("The list of hero settings that affects team 2."));
            heroesRoot.AdditionalProperties = false;

            // Add 'Description' property.
            root.Properties.Add("Description", new RootSchema("The description of the custom game.") {
                Type = SchemaObjectType.String
            });

            // Add 'Workshop' property.
            root.Properties.Add("Workshop", GetCustomSettingsSchema(generate));
            
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
            schema.AdditionalProperties = false;
            List<string> heroNames = new List<string>();
            foreach (var heroSettings in HeroSettingCollection.AllHeroSettings)
            {
                schema.Properties.Add(heroSettings.HeroName, heroSettings.GetSchema(generate));
                heroNames.Add(heroSettings.HeroName);
            }

            // Create the map schema.
            RootSchema heroes = new RootSchema {
                Type = SchemaObjectType.Array,
                UniqueItems = true,
                Items = new RootSchema() {
                    Type = SchemaObjectType.String,
                    Enum = heroNames.ToArray()
                }
            };
            // Add the map schema to the list of definitions.
            generate.Definitions.Add("All Heroes", heroes);

            RootSchema allHeroesReference = new RootSchema() { Ref = "#/definitions/All Heroes" };

            // Add the map schema reference to the current schema. 
            schema.Properties.Add("Enabled Heroes", allHeroesReference);
            schema.Properties.Add("Disabled Heroes", allHeroesReference);

            return schema;
        }

        private static RootSchema GetLobby(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema().InitProperties();
            schema.AdditionalProperties = false;
            foreach (var lobbySetting in LobbySettings) schema.Properties.Add(lobbySetting.Name, lobbySetting.GetSchema(generate));
            return schema;
        }

        private static RootSchema GetModes(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema().InitProperties();
            schema.AdditionalProperties = false;
            foreach (var mode in ModeSettingCollection.AllModeSettings) schema.Properties.Add(mode.ModeName, mode.GetSchema(generate));
            return schema;
        }
    
        private static RootSchema GetCustomSettingsSchema(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema().InitProperties();
            schema.AdditionalProperties = true;
            schema.Type = SchemaObjectType.Object;
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
            keywords.Add("General");
            keywords.Add("Team 1");
            keywords.Add("Team 2");
            keywords.Add("enabled maps");
            keywords.Add("disabled maps");
            keywords.Add("enabled heroes");
            keywords.Add("disabled heroes");
            keywords.Add("Enabled");
            keywords.Add("Disabled");
            keywords.Add("On");
            keywords.Add("Off");
            keywords.Add("Yes");
            keywords.Add("No");
            keywords.Add("main");
            keywords.Add("Description");
            keywords.Add(AbilityNameResolver.CooldownTime);
            keywords.Add(AbilityNameResolver.RechargeRate);
            keywords.Add(AbilityNameResolver.MaximumTime);
            keywords.Add(AbilityNameResolver.UltimateAbility);
            keywords.Add(AbilityNameResolver.UltimateGeneration);
            keywords.Add(AbilityNameResolver.UltimateGenerationCombat);
            keywords.Add(AbilityNameResolver.UltimateGenerationPassive);

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

        public static void WriteList(WorkshopBuilder builder, string[] maps)
        {
            builder.AppendLine("{");
            builder.Indent();

            foreach (string map in maps)
                builder.AppendLine(builder.Translate(map).RemoveStructuralChars());

            builder.Outdent();
            builder.AppendLine("}");
        }
    
        public static bool Validate(JObject jobject, FileDiagnostics diagnostics, DocRange range)
        {
            SettingValidation validation = new SettingValidation();

            // Check for invalid properties.
            foreach (JProperty setting in jobject.Properties())
                if (!new string[] { "Lobby", "Modes", "Heroes", "Description", "Workshop" }.Contains(setting.Name))
                    validation.InvalidSetting(setting.Name);
            
            // Check lobby settings.
            if (jobject.TryGetValue("Lobby", out JToken lobbySettings))
                ValidateSetting(validation, LobbySettings, lobbySettings);
            
            // Check modes.
            if (jobject.TryGetValue("Modes", out JToken modes))
                ModeSettingCollection.Validate(validation, (JObject)modes);
            
            // Check heroes.
            if (jobject.TryGetValue("Heroes", out JToken heroes))
                HeroesRoot.Validate(validation, (JObject)heroes);
            
            // Check description.
            if (jobject.TryGetValue("Description", out JToken description) && description.Type != JTokenType.String)
                validation.IncorrectType("Description", "string");

            validation.Dump(diagnostics, range);
            return !validation.HasErrors();
        }

        public static void ValidateSetting(SettingValidation validation, IEnumerable<LobbySetting> lobbySettings, JToken jobjectSettingContainer, params string[] additional)
        {
            // Iterate through all input lobby settings.
            foreach (JProperty lobbySetting in jobjectSettingContainer.Children<JProperty>())
            {
                if (additional != null && additional.Contains(lobbySetting.Name)) continue;

                // Get the related setting.
                LobbySetting relatedSetting = lobbySettings.FirstOrDefault(ls => ls.Name == lobbySetting.Name);

                // relatedSetting will be null if there are no settings with the name.
                if (relatedSetting == null) validation.InvalidSetting(lobbySetting.Name);
                // Validate the input value.
                else relatedSetting.CheckValue(validation, lobbySetting.Value);
            }
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

        public void ToWorkshop(WorkshopBuilder builder, List<LobbySetting> allSettings)
        {
            builder.AppendKeywordLine("heroes");
            builder.AppendLine("{");
            builder.Indent();

            if (General != null)
            {
                builder.AppendKeywordLine("General");
                builder.AppendLine("{");
                builder.Indent();
                General.ToWorkshop(builder, allSettings);
                builder.Outdent();
                builder.AppendLine("}");
            }
            if (Team1 != null)
            {
                builder.AppendKeywordLine("Team 1");
                builder.AppendLine("{");
                builder.Indent();
                Team1.ToWorkshop(builder, allSettings);
                builder.Outdent();
                builder.AppendLine("}");
            }
            if (Team2 != null)
            {
                builder.AppendKeywordLine("Team 2");
                builder.AppendLine("{");
                builder.Indent();
                Team2.ToWorkshop(builder, allSettings);
                builder.Outdent();
                builder.AppendLine("}");
            }

            builder.Outdent();
            builder.AppendLine("}");
        }

        public static void Validate(SettingValidation validation, JObject heroRoot)
        {
            if (heroRoot.TryGetValue("General", out JToken generalToken)) HeroSettingCollection.Validate(validation, (JObject)generalToken);
            if (heroRoot.TryGetValue("Team 1", out JToken team1Token)) HeroSettingCollection.Validate(validation, (JObject)team1Token);
            if (heroRoot.TryGetValue("Team 2", out JToken team2Token)) HeroSettingCollection.Validate(validation, (JObject)team2Token);
        }
    }

    public class WorkshopValuePair : Dictionary<String, object>
    {
        public void ToWorkshop(WorkshopBuilder builder, List<LobbySetting> allSettings)
        {
            ToWorkshop(this, builder, allSettings);
        }

        public void ToWorkshopCustom(WorkshopBuilder builder)
        {
            foreach (var setting in this)
            {
                string value = setting.Value.ToString();
                if (setting.Value is bool boolean)
                    value = boolean ? "On" : "Off";
                builder.AppendLine($"{setting.Key}: {value}");
            }
        }

        public static void ToWorkshop(Dictionary<String, object> dict, WorkshopBuilder builder, List<LobbySetting> allSettings)
        {
            foreach (var setting in dict)
            {
                // Get the related setting.
                LobbySetting relatedSetting = allSettings.FirstOrDefault(ls => ls.Name == setting.Key);

                string name = relatedSetting.ResolveName(builder);
                string value = relatedSetting.GetValue(builder, setting.Value);

                builder.AppendLine($"{name}: {value}");
            }
        }
    }

    public class HeroList
    {
        [JsonProperty("Enabled Heroes")]
        public string[] EnabledHeroes { get; set; }

        [JsonProperty("Disabled Heroes")]
        public string[] DisabledHeroes { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Settings { get; set; }

        public void ToWorkshop(WorkshopBuilder builder, List<LobbySetting> allSettings)
        {
            if (Settings != null)
                foreach (var hero in Settings)
                {
                    if (hero.Key != "General")
                    {
                        builder.AppendLine($"{hero.Key}");
                        builder.AppendLine("{");
                        builder.Indent();
                        WorkshopValuePair.ToWorkshop(((JObject)hero.Value).ToObject<Dictionary<string, object>>(), builder, allSettings);
                        builder.Outdent();
                        builder.AppendLine("}");
                    }
                    else WorkshopValuePair.ToWorkshop(((JObject)hero.Value).ToObject<Dictionary<string, object>>(), builder, allSettings);
                }
            if (EnabledHeroes != null)
            {
                builder.AppendLine();
                builder.AppendKeywordLine("enabled heroes");
                Ruleset.WriteList(builder, EnabledHeroes);
            }
            if (DisabledHeroes != null)
            {
                builder.AppendLine();
                builder.AppendKeywordLine("disabled heroes");
                Ruleset.WriteList(builder, DisabledHeroes);
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

        public virtual RootSchema GetSchema(SchemaGenerate generate)
        {
            RootSchema schema = new RootSchema(Title).InitProperties();
            schema.AdditionalProperties = false;
            foreach (var value in this) schema.Properties.Add(value.Name, value.GetSchema(generate));
            return schema;
        }

        public string[] GetKeywords()
        {
            List<string> keywords = new List<string>();

            foreach (LobbySetting setting in this)
            {
                keywords.AddRange(SettingNameResolver.Keywords(setting.Name));
                keywords.AddRange(setting.AdditionalKeywords());
            }

            return keywords.ToArray();
        }
    }

    public class SettingValidation
    {
        private readonly List<string> _errors = new List<string>();
        
        public SettingValidation() {}

        public void Error(string error)
        {
            _errors.Add(error);
        }

        public void InvalidSetting(string propertyName)
        {
            _errors.Add($"The setting '{propertyName}' is not valid.");
        }

        public void IncorrectType(string propertyName, string expectedType)
        {
            _errors.Add($"The setting '{propertyName}' requires a value of type " + expectedType + ".");
        }

        public bool HasErrors() => _errors.Count > 0;

        public void Dump(FileDiagnostics diagnostics, DocRange range)
        {
            foreach (string error in _errors) diagnostics.Error(error, range);
        }
    }
}