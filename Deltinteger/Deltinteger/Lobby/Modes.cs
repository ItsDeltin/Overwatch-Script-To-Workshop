using System;
using System.Collections.Generic;
using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.I18n;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Deltin.Deltinteger.Lobby
{
    public class ModesRoot
    {
        public WorkshopValuePair All { get; set; }
        
        public WorkshopValuePair Assault { get; set; }
        
        public WorkshopValuePair Control { get; set; }
        
        public WorkshopValuePair Escort { get; set; }
        
        public WorkshopValuePair Hybrid { get; set; }
        
        [JsonProperty("Capture The Flag")]
        public WorkshopValuePair CaptureTheFlag { get; set; }
        
        public WorkshopValuePair Deathmatch { get; set; }
        
        public WorkshopValuePair Elimination { get; set; }
        
        [JsonProperty("Team Deathmatch")]
        public WorkshopValuePair TeamDeathmatch { get; set; }
        
        public WorkshopValuePair Skirmish { get; set; }
        
        [JsonProperty("Practice Range")]
        public WorkshopValuePair PracticeRange { get; set; }

        public void MergeModeSettings()
        {
            if (All == null) return;
            foreach (var value in All)
            {
                MergeTo(value, Assault);
                MergeTo(value, CaptureTheFlag);
                MergeTo(value, Control);
                MergeTo(value, Deathmatch);
                MergeTo(value, Elimination);
                MergeTo(value, Escort);
                MergeTo(value, Hybrid);
                MergeTo(value, PracticeRange);
                MergeTo(value, Skirmish);
                MergeTo(value, TeamDeathmatch);
            }
        }

        private void MergeTo(KeyValuePair<string, object> pair, WorkshopValuePair set)
        {
            if (set == null || set.ContainsKey(pair.Key)) return;
            set.Add(pair.Key, pair.Value);
        }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            builder.AppendKeywordLine("modes");
            builder.AppendLine("{");
            builder.Indent();
            
            PrintMode(builder, "Assault", Assault);
            PrintMode(builder, "CaptureTheFlag", CaptureTheFlag);
            PrintMode(builder, "Control", Control);
            PrintMode(builder, "Deathmatch", Deathmatch);
            PrintMode(builder, "Elimination", Elimination);
            PrintMode(builder, "Escort", Escort);
            PrintMode(builder, "Hybrid", Hybrid);
            PrintMode(builder, "PracticeRange", PracticeRange);
            PrintMode(builder, "Skirmish", Skirmish);
            PrintMode(builder, "TeamDeathmatch", TeamDeathmatch);

            builder.Unindent();
            builder.AppendLine("}");
        }

        private static void PrintMode(WorkshopBuilder builder, string modeName, WorkshopValuePair mode)
        {
            if (mode == null) return;
            if (!mode.TryGetValue("Enabled", out object value) || value as bool? == false) return;
            mode.Remove("Enabled");
            builder.AppendKeywordLine(modeName);
            builder.AppendLine("{");
            builder.Indent();
            mode.ToWorkshop(builder);
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
            new SelectValue("Hero Limit", "1 Per Team", "2 Per Team", "1 Per Game", "2 Per Game"),
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

        public static void Init()
        {
            AllModeSettings = new ModeSettingCollection[] {
                new ModeSettingCollection("All"),
                new ModeSettingCollection("Assault", true).Competitive().AddCaptureSpeed(),
                new ModeSettingCollection("Control", true).Competitive().AddCaptureSpeed().AddSelect("Limit Valid Control Points", "All", "First", "Second", "Third").AddIntRange("Score To Win", 1, 3, 2, "Score To Win 1-3").AddRange("Scoring Speed Modifier", 10, 500),
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
                new ModeSettingCollection("Skirmish", false),
                new ModeSettingCollection("Practice Range", false).AddSwitch("Spawn Training Bots", true).AddRange("Training Bot Respawn Time Scalar", 10, 500)
            };
        }
    }
}