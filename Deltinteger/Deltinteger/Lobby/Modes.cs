using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Lobby
{
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

        public static ModeSettingCollection[] AllModeSettings = new ModeSettingCollection[] {
            new ModeSettingCollection("General"),
            new ModeSettingCollection("Assault", true).Competitive().AddCaptureSpeed(),
            new ModeSettingCollection("Control", true).Competitive().AddCaptureSpeed().AddSelect("Limit Valid Control Points", "All", "First", "Second", "Third").AddIntRange("Score To Win", 1, 3, 2).AddRange("Scoring Speed Modifier", 10, 500),
            new ModeSettingCollection("Escort", true).Competitive().AddPayloadSpeed(),
            new ModeSettingCollection("Hybrid", true).Competitive().AddCaptureSpeed().AddPayloadSpeed(),
        };

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
            Add(new SwitchValue("Enabled", defaultEnabled));
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
    }
}