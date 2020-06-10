using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.I18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby
{
    public class ModesRoot
    {
        public WorkshopValuePair All { get; set; }
        
        public ModeSettings Assault { get; set; }
        
        public ModeSettings Control { get; set; }
        
        public ModeSettings Escort { get; set; }
        
        public ModeSettings Hybrid { get; set; }
        
        [JsonProperty("Capture The Flag")]
        public ModeSettings CaptureTheFlag { get; set; }
        
        public ModeSettings Deathmatch { get; set; }
        
        public ModeSettings Elimination { get; set; }
        
        [JsonProperty("Team Deathmatch")]
        public ModeSettings TeamDeathmatch { get; set; }
        
        public ModeSettings Skirmish { get; set; }
        
        [JsonProperty("Practice Range")]
        public ModeSettings PracticeRange { get; set; }

        public void ToWorkshop(WorkshopBuilder builder, List<LobbySetting> allSettings)
        {
            builder.AppendKeywordLine("modes");
            builder.AppendLine("{");
            builder.Indent();

            if (All != null)
            {
                builder.AppendKeywordLine("General");
                builder.AppendLine("{");
                builder.Indent();
                All.ToWorkshop(builder, allSettings);
                builder.Unindent();
                builder.AppendLine("}");
            }
            
            Assault?.ToWorkshop(builder, allSettings, "Assault");
            CaptureTheFlag?.ToWorkshop(builder, allSettings, "CaptureTheFlag");
            Control?.ToWorkshop(builder, allSettings, "Control");
            Deathmatch?.ToWorkshop(builder, allSettings, "Deathmatch");
            Elimination?.ToWorkshop(builder, allSettings, "Elimination");
            Escort?.ToWorkshop(builder, allSettings, "Escort");
            Hybrid?.ToWorkshop(builder, allSettings, "Hybrid");
            PracticeRange?.ToWorkshop(builder, allSettings, "PracticeRange");
            Skirmish?.ToWorkshop(builder, allSettings, "Skirmish");
            TeamDeathmatch?.ToWorkshop(builder, allSettings, "TeamDeathmatch");

            builder.Unindent();
            builder.AppendLine("}");
        }
    }

    public class ModeSettings
    {
        [JsonProperty("Enabled Maps")]
        public string[] EnabledMaps { get; set; }

        [JsonProperty("Disabled Maps")]
        public string[] DisabledMaps { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Settings { get; set; }

        public void ToWorkshop(WorkshopBuilder builder, List<LobbySetting> allSettings, string modeName)
        {
            builder.AppendKeywordLine(modeName);

            Settings?.Remove("Enabled");

            if (EnabledMaps != null || DisabledMaps != null || (Settings != null && Settings.Count > 0))
            {
                builder.AppendLine("{");
                builder.Indent();

                if (Settings != null) WorkshopValuePair.ToWorkshop(Settings, builder, allSettings);

                if (EnabledMaps != null)
                {
                    builder.AppendKeywordLine("enabled maps");
                    Ruleset.WriteList(builder, EnabledMaps);
                }
                if (DisabledMaps != null)
                {
                    builder.AppendKeywordLine("disabled maps");
                    Ruleset.WriteList(builder, DisabledMaps);
                }

                builder.Unindent();
                builder.AppendLine("}");
            }
        }

        public static void WriteList(WorkshopBuilder builder, string[] maps)
        {
            builder.AppendLine("{");
            builder.Indent();

            foreach (string map in maps)
                builder.AppendLine(builder.Translate(map).RemoveStructuralChars());

            builder.Unindent();
            builder.AppendLine("}");
        }
    }
    
    public class ModeSettingCollection : LobbySettingCollection<ModeSettingCollection>
    {
        private static LobbySetting[] DefaultModeSettings = new LobbySetting[]
        {
            new SwitchValue("Enemy Health Bars", true),
            new SelectValue("Game Mode Start", "All Slots Filled", "Immediately", "Manual"),
            new RangeValue("Health Pack Respawn Time Scalar", 10, 500),
            new SwitchValue("Kill Cam", true),
            new SwitchValue("Kill Feed", true),
            new SwitchValue("Skins", true),
            new SelectValue("Spawn Health Packs", "Determined By Mode", "Enabled", "Disabled"),
            new SwitchValue("Allow Hero Switching", true),
            new SelectValue("Hero Limit", "1 Per Team", "2 Per Team", "1 Per Game", "2 Per Game", "Off"),
            new SelectValue("Limit Roles", "2 Of Each Role Per Team", "Off"),
            new SwitchValue("Respawn As Random Hero", false),
            new RangeValue("Respawn Time Scalar", 0, 100)
        };
        private static LobbySetting CaptureSpeed = new RangeValue("Capture Speed Modifier", 10, 500);
        private static LobbySetting PayloadSpeed = new RangeValue("Payload Speed Modifier", 10, 500);
        private static LobbySetting CompetitiveRules = new SwitchValue("Competitive Rules", false);
        private static LobbySetting Enabled_DefaultOn = new SwitchValue("Enabled", true) { ReferenceName = "Enabled On" };
        private static LobbySetting Enabled_DefaultOff = new SwitchValue("Enabled", false) { ReferenceName = "Enabled Off" };
        private static LobbySetting GameLengthInMinutes = new RangeValue(false, "Game Length In Minutes", 5, 15, 10);
        private static LobbySetting SelfInitiatedRespawn = new SwitchValue("Self Initiated Respawn", true);
        private static LobbySetting ScoreToWin_1to9 = new RangeValue(false, "Score To Win", 1, 9, 3) { ReferenceName = "Score To Win 1-9" };
        private static LobbySetting LimitValidControlPoints = new SelectValue("Limit Valid Control Points", "All", "First", "Second", "Third");

        public static ModeSettingCollection[] AllModeSettings { get; private set; }

        public string ModeName { get; }

        public ModeSettingCollection(string title)
        {
            ModeName = title;
            Title = title;
            AddRange(DefaultModeSettings);
        }

        public ModeSettingCollection(string modeName, bool defaultEnabled)
        {
            ModeName = modeName;
            Title = $"{ModeName} settings.";

            if (defaultEnabled) Add(Enabled_DefaultOn);
            else Add(Enabled_DefaultOff);

            AddRange(DefaultModeSettings);
        }

        public ModeSettingCollection AddCaptureSpeed()
        {
            Add(CaptureSpeed);
            return this;
        }

        public ModeSettingCollection AddPayloadSpeed()
        {
            Add(PayloadSpeed);
            return this;
        }

        public ModeSettingCollection Competitive()
        {
            Add(CompetitiveRules);
            return this;
        }

        public override RootSchema GetSchema(SchemaGenerate generate)
        {
            RootSchema schema = base.GetSchema(generate);
            schema.AdditionalProperties = false;

            // Get the mode's maps.
            string[] modeMaps = LobbyMap.AllMaps.Where(map => map.GameModes.Contains(ModeName)).Select(map => map.Name).ToArray();
            if (modeMaps.Length == 0) return schema;

            // Create the map schema.
            RootSchema maps = new RootSchema {
                Type = SchemaObjectType.Array,
                UniqueItems = true,
                Items = new RootSchema() {
                    Type = SchemaObjectType.String,
                    Enum = modeMaps
                }
            };
            // Add the map schema to the list of definitions.
            generate.Definitions.Add(ModeName + " Maps", maps);

            // Add the map schema reference to the current schema. 
            schema.Properties.Add("Enabled Maps", GetMapReference("An array of enabled maps for the '" + ModeName + "' mode."));
            schema.Properties.Add("Disabled Maps", GetMapReference("An array of disabled maps for the '" + ModeName + "' mode."));
            
            return schema;
        }

        private RootSchema GetMapReference(string description)
        {
            return new RootSchema(description) { Ref = "#/definitions/" + ModeName + " Maps" };
        }

        public static void Init()
        {
            AllModeSettings = new ModeSettingCollection[] {
                new ModeSettingCollection("All"),
                new ModeSettingCollection("Assault", true).Competitive().AddCaptureSpeed(),
                new ModeSettingCollection("Control", true).Competitive().AddCaptureSpeed().Add(LimitValidControlPoints).AddIntRange("Score To Win", 1, 3, 2, "Score To Win 1-3").AddRange("Scoring Speed Modifier", 10, 500),
                new ModeSettingCollection("Escort", true).Competitive().AddPayloadSpeed(),
                new ModeSettingCollection("Hybrid", true).Competitive().AddCaptureSpeed().AddPayloadSpeed(),
                new ModeSettingCollection("Capture The Flag", false).AddSwitch("Blitz Flag Locations", false).AddSwitch("Damage Interrupts Flag Interaction", false)
                    .AddSelect("Flag Carrier Abilities", "Restricted", "All", "None").AddRange("Flag Dropped Lock Time", 0, 10, 5).AddRange("Flag Pickup Time", 0, 5, 0).AddRange("Flag Return Time", 0, 5, 4)
                    .AddRange("Flag Score Respawn Time", 0, 20, 15).AddIntRange("Game Length (Minutes)", 5, 15, 8).AddRange("Respawn Speed Buff Duration", 0, 60, 0).Add(ScoreToWin_1to9)
                    .AddSwitch("Team Needs Flag At Base To Score", false),
                new ModeSettingCollection("Deathmatch", false).Add(GameLengthInMinutes).Add(SelfInitiatedRespawn).AddIntRange("Score To Win", 1, 50, 20, "Score To Win 1-50"),
                new ModeSettingCollection("Elimination", false).AddRange("Hero Selection Time", 20, 60, 20).Add(ScoreToWin_1to9).AddSelect("Restrict Previously Used Heroes", "Off", "After Round Won", "After Round Played")
                    .AddSelect("Hero Selection", "Any", "Limited", "Random", "Random (Mirrored)").AddSelect("Limited Choice Pool", "Team Size +2", "Team Size", "Team Size +1", "Team Size +3")
                    .AddSwitch("Capture Objective Tiebreaker", true).AddIntRange("Tiebreaker After Match Time Elapsed", 30, 300, 105).AddIntRange("Time To Capture", 1, 7, 3).AddIntRange("Draw After Match Time Elapsed With No Tiebreaker", 60, 300, 135)
                    .AddSwitch("Reveal Heroes", false).AddIntRange("Reveal Heroes After Match Time Elapsed", 0, 180, 75),
                new ModeSettingCollection("Team Deathmatch", false).Add(GameLengthInMinutes).AddSwitch("Mercy Resurrect Counteracts Kills", true).AddIntRange("Score To Win", 1, 200, 30, "Score To Win 1-200").Add(SelfInitiatedRespawn).AddSwitch("Imbalanced Team Score To Win", false)
                    .AddIntRange("Team 1 Score To Win", 1, 200, 30).AddIntRange("Team 2 Score To Win", 1, 200, 30),
                new ModeSettingCollection("Skirmish", false).Add(LimitValidControlPoints),
                new ModeSettingCollection("Practice Range", false).AddSwitch("Spawn Training Bots", true).AddRange("Training Bot Respawn Time Scalar", 10, 500)
            };
        }
    
        public static void Validate(SettingValidation validation, JObject modes)
        {
            foreach (var modeCollection in AllModeSettings)
                if (modes.TryGetValue(modeCollection.ModeName, out JToken modeSettingsToken))
                    Ruleset.ValidateSetting(validation, modeCollection, modeSettingsToken, "Enabled Maps", "Disabled Maps");
        }
    }
}