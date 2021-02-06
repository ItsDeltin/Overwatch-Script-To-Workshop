using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using static Deltin.Deltinteger.Lobby.LobbyConstants;

namespace Deltin.Deltinteger.Lobby.Deserializer
{
    class RulesetReader
    {
        public string Description { get; private set; }
        public SettingPairCollection LobbySettings { get; private set; }
        public ModesRoot ModeSettings { get; private set; }
        public HeroesRoot HeroSettings { get; private set; }

        public Ruleset GetRuleset() => new Ruleset() {
            Description = Description,
            Lobby = LobbySettings,
            Modes = ModeSettings,
            Heroes = HeroSettings
        };

        public void ReadRootProp(JProperty prop)
        {
            Action<JToken> propReader = prop.Name switch {
                DESCRIPTION => ReadDescription,
                LOBBY => ReadLobbySettings,
                MODES => ReadModeList,
                HEROES => ReadHeroes,
                WORKSHOP => ReadWorkshop,
                _ => throw new NotImplementedException()
            };
            propReader(prop.Value);
        }

        void ReadDescription(JToken value)
        {
            Description = value.ToObject<string>();
        }

        // * "Lobby": { ... } *
        void ReadLobbySettings(JToken value)
        {
            var settings = new List<SettingPair>();
            ForEachObjectChildProperty(value, prop => {
                if (TryGetPropertySetting(prop, Ruleset.LobbySettings, out var setting))
                    settings.Add(setting);
                else; // todo: invalid setting
            });
            LobbySettings = new SettingPairCollection(settings);
        }

        // * "Heroes": { ... } *
        void ReadHeroes(JToken value)
        {
            var heroGroups = new string[] { GENERAL, TEAM_1, TEAM_2 };
            HeroTeamGroup general = null, team1 = null, team2 = null;

            ForEachObjectChildProperty(value, prop => {
                var heroGroups = new string[] { GENERAL, TEAM_1, TEAM_2 };
                for (int i = 0; i < heroGroups.Length; i++)
                {
                    if (prop.Name == heroGroups[i])
                    {
                        var group = ReadHeroGroup(prop.Value);

                        if (i == 0)
                            general = group;
                        else if (i == 1)
                            team1 = group;
                        else if (i == 2)
                            team2 = group;
                    }
                }
            });

            HeroSettings = new HeroesRoot() {
                General = general,
                Team1 = team1,
                Team2 = team2
            };
        }

        HeroTeamGroup ReadHeroGroup(JToken value)
        {
            var heroes = new List<HeroSettingGroup>();
            string[] enabledHeroes = null;
            string[] disabledHeroes = null;

            // Loop through every child.
            ForEachObjectChildProperty(value, prop => {
                // Hero collection.
                if (ReadHeroSpecificSettings(prop, out var heroSettings))
                    heroes.Add(heroSettings);

                // Enabled heroes.
                else if (prop.Name == ENABLED_HEROES)
                    enabledHeroes = ExtractStringArray(prop.Value, IsValidHeroName);

                // Disabled heroes.
                else if (prop.Name == DISABLED_HEROES)
                    disabledHeroes = ExtractStringArray(prop.Value, IsValidHeroName);
                
                // todo: not a valid property name
            });
            return new HeroTeamGroup(heroes.ToArray(), enabledHeroes, disabledHeroes);
        }

        bool ReadHeroSpecificSettings(JProperty prop, out HeroSettingGroup result)
        {
            /*
            'prop' can be formatted as so:
            "Ana": { "Health": 100 }
            */
            // Get the matching hero ("Ana")
            var matchingHero = GetHeroCollection(prop.Name);

            // Not a hero.
            if (matchingHero == null)
            {
                result = null;
                return false;
            }

            // Loop through the settings. ({ "Health": 100 })
            var settings = new List<SettingPair>();
            ForEachObjectChildProperty(prop.Value, prop => {
                if (TryGetPropertySetting(prop, matchingHero, out var setting))
                    settings.Add(setting);
                else; // TODO: Unknown setting name error
            });
            
            result = new HeroSettingGroup(matchingHero.CollectionName, new SettingPairCollection(settings));
            return true;
        }

        // * "Modes": { ... } *
        void ReadModeList(JToken value)
        {
            var modeList = new List<ModeSettings>();

            ForEachObjectChildProperty(value, prop => {
                // Get the mode of this property.
                var matchingMode = GetMatchingModeCollection(prop.Name);

                // If a matching mode was found, read the settings.
                if (matchingMode != null)
                    modeList.Add(ReadModeSettings(matchingMode, prop.Value));
                else; // TODO: Unknown mode name error
            });

            ModeSettings = new ModesRoot(modeList.ToArray());
        }

        ModeSettings ReadModeSettings(ModeSettingCollection collection, JToken value)
        {
            var settings = new List<SettingPair>();
            string[] enabledMaps = null;
            string[] disabledMaps = null;

            // Loop through each property.
            ForEachObjectChildProperty(value, prop => {
                // If the property is a valid LobbySetting, copy it.
                if (TryGetPropertySetting(prop, collection, out var setting))
                    settings.Add(setting);
                // Enabled maps
                else if (prop.Name == ENABLED_MAPS)
                    enabledMaps = ExtractStringArray(prop.Value, IsValidMapName);
                // Disabled maps
                else if (prop.Name == DISABLED_MAPS)
                    disabledMaps = ExtractStringArray(prop.Value, IsValidMapName);
                else; // TODO: Error unknown property
            });

            return new ModeSettings(collection.CollectionName, enabledMaps, disabledMaps, settings.ToArray());
        }

        bool IsValidHeroName(string name) => HeroSettingCollection.AllHeroSettings.Any(hero => hero.CollectionName == name);
        bool IsValidMapName(string name) => throw new NotImplementedException();
        ModeSettingCollection GetMatchingModeCollection(string modeName) => ModeSettingCollection.AllModeSettings.FirstOrDefault(collection => collection.CollectionName == modeName);
        HeroSettingCollection GetHeroCollection(string heroName) => HeroSettingCollection.AllHeroSettings.FirstOrDefault(collection => collection.CollectionName == heroName);

        // * "Workshop": { ... } *
        void ReadWorkshop(JToken value) => throw new NotImplementedException();

        void ForEachObjectChildProperty(JToken obj, Action<JProperty> onProp)
        {
            // Execute on every child.
            foreach (var child in obj)
                // Ensure that the child is a property.
                if (child is JProperty prop)
                    onProp(prop);
                // todo: error when not a property.
        }

        T[] ForEachObjectChildProperty<T>(JToken obj, Func<JProperty, T> onProp)
        {
            var list = new List<T>();
            ForEachObjectChildProperty(obj, prop => list.Add(onProp(prop)));
            return list.ToArray();
        }

        string[] ExtractStringArray(JToken token, Func<string, bool> isValid)
        {
            var items = new List<string>();

            // Loop through every hero in the array.
            foreach (var child in token)
            {
                // Convert the hero token to a string.
                string name = child.ToObject<string>();

                // Make sure the hero actually exists.
                if (isValid(name))
                    items.Add(name);
                // todo: invalid name
            }

            return items.ToArray();
        }

        bool TryGetPropertySetting(JProperty prop, IReadOnlyCollection<LobbySetting> lobbySettings, out SettingPair setting)
        {
            foreach (var lobbySetting in lobbySettings)
                if (prop.Name == lobbySetting.Name)
                {
                    setting = new SettingPair(prop.Name, prop.Value.ToObject<object>());
                    return true;
                }
            
            setting = null;
            return false;
        }

        bool IsValidSetting(LobbySetting[] lobbySettings, string name) => lobbySettings.Any(ls => ls.Name == name);

        bool TryGetPropertyValue(JToken token, string name, out JToken value)
        {
            foreach (var child in token)
                if (child is JProperty prop && prop.Name == name)
                {
                    value = prop.Value;
                    return true;
                }
            
            value = null;
            return false;
        }
    }
}