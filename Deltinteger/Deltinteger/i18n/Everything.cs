using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;

namespace Deltin.Deltinteger.Everything
{
    class GenerateEverythingScript
    {
        private readonly List<KeywordGroup> _keywordGroups = new List<KeywordGroup>();

        public static void Generate()
        {

        }

        void GenerateRuleset()
        {
            var lobbySettingsGroup = new KeywordGroup();

            // Loop through each lobby setting.
            foreach (var setting in Ruleset.LobbySettings)
            {
                var value = GetNondefaultSettingValue(setting);
                lobbySettingsGroup.Add(new KeywordToken(setting.Name));
            }
        }

        static object GetNondefaultSettingValue(LobbySetting setting)
        {
            switch (setting)
            {
                // [0] is the default.
                case SelectValue select: return select.Values[1];
                // Invert default.
                case SwitchValue witch: return !witch.Default;
                // Number range.
                case RangeValue range:
                    // If the minimum value is not the default, use the minimum.
                    if (range.Min != range.Default)
                        return range.Min;
                    // Otherwise, use the maximum.
                    else if (range.Max != range.Default)
                        return range.Max;
                    // This can happen if a range value's minimum and maximum are the same,
                    // which means that it is a setting that cannot be changed.
                    // Right now this can only happen with a certain bounty hunter setting.
                    else
                        return range.Default;
                
                default: throw new NotImplementedException();
            }
        }
    }

    class KeywordGroup
    {
        private readonly List<KeywordToken> _keywords = new List<KeywordToken>();

        public void Add(KeywordToken keyword)
        {
            _keywords.Add(keyword);
        }
    }

    class KeywordToken
    {
        public string Name { get; }

        public KeywordToken(string name)
        {
            Name = name;
        }
    }
}