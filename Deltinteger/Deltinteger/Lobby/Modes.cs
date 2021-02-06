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
        public ModeSettings[] Modes { get; set; }
        public SettingPairCollection GeneralSettings { get; set; }

        public ModesRoot(IList<ModeSettings> modes)
        {
            Modes = modes.ToArray();
        }

        public void ToWorkshop(WorkshopBuilder builder, IReadOnlyCollection<LobbySetting> allSettings)
        {
            builder.AppendKeywordLine("modes");
            builder.AppendLine("{");
            builder.Indent();

            foreach (var mode in Modes)
                mode.ToWorkshop(builder, allSettings);

            builder.Outdent();
            builder.AppendLine("}");
        }
    }

    public class ModeSettings
    {
        public string ModeName { get; set; }
        public string[] EnabledMaps { get; set; }
        public string[] DisabledMaps { get; set; }
        public SettingPair[] Settings { get; set; }
        public bool Enabled { get; set; }
        bool IsGeneral => ModeName == "All";

        public ModeSettings(string modeName)
        {
            ModeName = modeName;
        }
        public ModeSettings(string modeName, string[] enabledMaps, string[] disabledMaps, SettingPair[] settings)
        {
            ModeName = modeName;
            EnabledMaps = enabledMaps;
            DisabledMaps = disabledMaps;
            Settings = settings;
        }

        public void ToWorkshop(WorkshopBuilder builder, IReadOnlyCollection<LobbySetting> allSettings)
        {
            // General settings.
            if (IsGeneral)
            {
                builder.AppendKeywordLine("General");
                builder.AppendLine("{");
                builder.Indent();
                
                // Write settings.
                if (Settings != null)
                    foreach (var setting in Settings)
                        setting.ToWorkshop(builder, allSettings);

                builder.Outdent();
                builder.AppendLine("}");
            }
            else // Mode settings
            {
                bool enabled = Settings == null || !SettingPair.TryGetValue(Settings, "Enabled", out object value) || (value is bool b && b);

                if (!enabled) builder.AppendKeyword("disabled").Append(" ");
                builder.AppendKeywordLine(ModeName);

                if (EnabledMaps != null || DisabledMaps != null || (Settings != null && Settings.Length > 0))
                {
                    builder.AppendLine("{");
                    builder.Indent();

                    // Write settings.
                    if (Settings != null)
                        foreach (var setting in Settings)
                        {
                            // Do not write the enabled setting.
                            if (setting.Name == "Enabled") continue;
                            setting.ToWorkshop(builder, allSettings);
                        }

                    // Write enabled maps.
                    if (EnabledMaps != null)
                    {
                        builder.AppendKeywordLine("enabled maps");
                        Ruleset.WriteList(builder, EnabledMaps);
                    }

                    // Write disabled maps.
                    if (DisabledMaps != null)
                    {
                        builder.AppendKeywordLine("disabled maps");
                        Ruleset.WriteList(builder, DisabledMaps);
                    }

                    builder.Outdent();
                    builder.AppendLine("}");
                }
            }
        }
    }

    public class ModeSettingCollection : LobbySettingCollection<ModeSettingCollection>
    {
        private static readonly LobbySetting[] DefaultModeSettings = new LobbySetting[]
        {
            new SwitchValue("Enemy Health Bars", true),
            new SelectValue("Game Mode Start", "All Slots Filled", "Immediately", "Manual"),
            new RangeValue(false, true, "Health Pack Respawn Time Scalar", 10, 500),
            new SwitchValue("Kill Cam", true),
            new SwitchValue("Kill Feed", true),
            new SwitchValue("Skins", true),
            new SelectValue("Spawn Health Packs", "Determined By Mode", "Enabled", "Disabled"),
            new SwitchValue("Allow Hero Switching", true),
            new SelectValue("Hero Limit", "1 Per Team", "2 Per Team", "1 Per Game", "2 Per Game", "Off"),
            new SelectValue("Limit Roles", "2 Of Each Role Per Team", "Off"),
            new SwitchValue("Respawn As Random Hero", false),
            new RangeValue(false, true, "Respawn Time Scalar", 0, 100)
        };
        private static readonly LobbySetting CaptureSpeed = new RangeValue(false, true, "Capture Speed Modifier", 10, 500);
        private static readonly LobbySetting PayloadSpeed = new RangeValue(false, true, "Payload Speed Modifier", 10, 500);
        private static readonly LobbySetting CompetitiveRules = new SwitchValue("Competitive Rules", false);
        private static readonly LobbySetting Enabled_DefaultOn = new SwitchValue("Enabled", true) { ReferenceName = "Enabled On" };
        private static readonly LobbySetting Enabled_DefaultOff = new SwitchValue("Enabled", false) { ReferenceName = "Enabled Off" };
        private static readonly LobbySetting GameLengthInMinutes = new RangeValue(true, false, "Game Length In Minutes", 5, 15, 10);
        private static readonly LobbySetting SelfInitiatedRespawn = new SwitchValue("Self Initiated Respawn", true);
        private static readonly LobbySetting ScoreToWin_1to9 = new RangeValue(false, false, "Score To Win", 1, 9, 3) { ReferenceName = "Score To Win 1-9" };
        private static readonly LobbySetting LimitValidControlPoints = new SelectValue("Limit Valid Control Points", "All", "First", "Second", "Third");
        private static readonly LobbySetting HeroSelectionTime = new RangeValue(true, false, "Hero Selection Time", 20, 60, 20);
        private static readonly LobbySetting RestrictPreviouslyUsedHeroes = new SelectValue("Restrict Previously Used Heroes", "Off", "After Round Won", "After Round Played");
        private static readonly LobbySetting HeroSelection = new SelectValue("Hero Selection", "Any", "Limited", "Random", "Random (Mirrored)");
        private static readonly LobbySetting LimitedChoicePool = new SelectValue("Limited Choice Pool", "Team Size +2", "Team Size", "Team Size +1", "Team Size +3");
        private static readonly LobbySetting CaptureObjectiveTiebreaker = new SwitchValue("Capture Objective Tiebreaker", true);
        private static readonly LobbySetting TiebreakerAfterTimeElapsed = new RangeValue(true, false, "Tiebreaker After Match Time Elapsed", 30, 300, 105);
        private static readonly LobbySetting TimeToCapture = new RangeValue(true, false, "Time To Capture", 1, 7, 3);
        private static readonly LobbySetting DrawAfterMatchTimeElaspedWithNoTiebreaker = new RangeValue(true, false, "Draw After Match Time Elapsed With No Tiebreaker", 60, 300, 135);
        private static readonly LobbySetting RevealHeroes = new SwitchValue("Reveal Heroes", false);
        private static readonly LobbySetting RevealHeroesAfterMatchTimeElapsed = new RangeValue(true, false, "Reveal Heroes After Match Time Elapsed", 0, 180, 75);

        public static ModeSettingCollection[] AllModeSettings { get; private set; }

        public ModeSettingCollection(string title) : base(title)
        {
            Title = title;
            AddAll(DefaultModeSettings);
        }

        public ModeSettingCollection(string modeName, bool defaultEnabled) : base(modeName)
        {
            Title = $"{CollectionName} settings.";

            if (defaultEnabled) Add(Enabled_DefaultOn);
            else Add(Enabled_DefaultOff);

            AddAll(DefaultModeSettings);
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

        public ModeSettingCollection Elimination() => Add(HeroSelectionTime).Add(ScoreToWin_1to9).Add(RestrictPreviouslyUsedHeroes).Add(HeroSelection).Add(LimitedChoicePool)
            .Add(CaptureObjectiveTiebreaker).Add(TiebreakerAfterTimeElapsed).Add(TimeToCapture).Add(DrawAfterMatchTimeElaspedWithNoTiebreaker).Add(RevealHeroes).Add(RevealHeroesAfterMatchTimeElapsed);

        public override RootSchema GetSchema(SchemaGenerate generate)
        {
            RootSchema schema = base.GetSchema(generate);
            schema.AdditionalProperties = false;

            // Get the mode's maps.
            string[] modeMaps = LobbyMap.AllMaps.Where(map => map.GameModes.Any(mode => mode.ToLower() == CollectionName.ToLower())).Select(map => map.Name).ToArray();
            if (modeMaps.Length == 0) return schema;

            // Create the map schema.
            RootSchema maps = new RootSchema
            {
                Type = SchemaObjectType.Array,
                UniqueItems = true,
                Items = new RootSchema()
                {
                    Type = SchemaObjectType.String,
                    Enum = modeMaps
                }
            };
            // Add the map schema to the list of definitions.
            generate.Definitions.Add(CollectionName + " Maps", maps);

            // Add the map schema reference to the current schema. 
            schema.Properties.Add("Enabled Maps", GetMapReference("An array of enabled maps for the '" + CollectionName + "' mode."));
            schema.Properties.Add("Disabled Maps", GetMapReference("An array of disabled maps for the '" + CollectionName + "' mode."));

            return schema;
        }

        private RootSchema GetMapReference(string description)
        {
            return new RootSchema(description) { Ref = "#/definitions/" + CollectionName + " Maps" };
        }

        public static void Init()
        {
            AllModeSettings = new ModeSettingCollection[] {
                new ModeSettingCollection("All"),
                new ModeSettingCollection("Assault", true).Competitive().AddCaptureSpeed(),
                new ModeSettingCollection("Control", true).Competitive().AddCaptureSpeed().Add(LimitValidControlPoints).AddIntRange("Score To Win", false, 1, 3, 2, "Score To Win 1-3").AddRange("Scoring Speed Modifier", 10, 500),
                new ModeSettingCollection("Escort", true).Competitive().AddPayloadSpeed(),
                new ModeSettingCollection("Hybrid", true).Competitive().AddCaptureSpeed().AddPayloadSpeed(),
                new ModeSettingCollection("Capture The Flag", false).AddSwitch("Blitz Flag Locations", false).AddSwitch("Damage Interrupts Flag Interaction", false)
                    .AddSelect("Flag Carrier Abilities", "Restricted", "All", "None").AddRange("Flag Dropped Lock Time", 0, 10, 5).AddRange("Flag Pickup Time", 0, 5, 0).AddRange("Flag Return Time", 0, 5, 4)
                    .AddRange("Flag Score Respawn Time", 0, 20, 15).AddIntRange("Game Length (Minutes)", false, 5, 15, 8).AddRange("Respawn Speed Buff Duration", 0, 60, 0).Add(ScoreToWin_1to9)
                    .AddSwitch("Team Needs Flag At Base To Score", false),
                new ModeSettingCollection("Deathmatch", false).Add(GameLengthInMinutes).Add(SelfInitiatedRespawn).AddIntRange("Score To Win", false, 1, 50, 20, "Score To Win 1-50"),
                new ModeSettingCollection("Elimination", false).Elimination(),
                new ModeSettingCollection("Team Deathmatch", false).Add(GameLengthInMinutes).AddSwitch("Mercy Resurrect Counteracts Kills", true).AddIntRange("Score To Win", false, 1, 200, 30, "Score To Win 1-200").Add(SelfInitiatedRespawn).AddSwitch("Imbalanced Team Score To Win", false)
                    .AddIntRange("Team 1 Score To Win", false, 1, 200, 30).AddIntRange("Team 2 Score To Win", false, 1, 200, 30),
                new ModeSettingCollection("Skirmish", false).Add(LimitValidControlPoints),
                new ModeSettingCollection("Practice Range", false).AddSwitch("Spawn Training Bots", true).AddRange("Training Bot Respawn Time Scalar", 10, 500),
                new ModeSettingCollection("Freezethaw Elimination", false).Elimination()
            };

            // Get re-occurring settings.
            var encountered = new HashSet<string>(); // Setting keys that were encountered.
            var reoccuring = new HashSet<LobbySetting>(); // Setting keys that were repeatedly encountered.
            var ignore = new LobbySetting[] { Enabled_DefaultOn, Enabled_DefaultOff };

            foreach (var modeCollection in AllModeSettings)
                foreach (var setting in modeCollection)
                    // Make sure that the setting is not inside the 'ignored' array.
                    // 'encountered' will return false if the key was already added to the HashSet. In this case, we can just 
                    if (!ignore.Contains(setting) && !encountered.Add(setting.ReferenceName))
                        reoccuring.Add(setting);

            // Add reocurring settings to general.
            AllModeSettings[0].AddAll(reoccuring);
        }

        public static void Validate(SettingValidation validation, JObject modes)
        {
            foreach (var modeCollection in AllModeSettings)
                if (modes.TryGetValue(modeCollection.CollectionName, out JToken modeSettingsToken))
                    Ruleset.ValidateSetting(validation, modeCollection, modeSettingsToken, "Enabled Maps", "Disabled Maps");
        }
    }
}
