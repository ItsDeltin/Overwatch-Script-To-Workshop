using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Lobby;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    partial class ConvertTextToElement
    {
        public bool MatchSettings()
        {
            if (!Match(Kw("settings"))) return false;

            Ruleset ruleset = new Ruleset();

            Match("{"); // Start settings section.

            // Main settings
            if (Match(Kw("main")))
            {
                Match("{"); // Start main section.

                // Description
                if (Match(Kw("Description") + ":"))
                {
                    MatchString(out string description);
                    ruleset.Description = description;
                }

                Match("}"); // End main section.
            }

            // General lobby settings
            if (Match(Kw("lobby")))
            {
                var lobbySettings = new List<SettingPair>();
                Match("{"); // Start lobby section.
                GroupSettings(lobbySettings, Ruleset.LobbySettings); // Match the settings and value pairs.
                Match("}"); // End lobby section.
                ruleset.Lobby = new SettingPairCollection(lobbySettings);
            }

            // Modes
            if (Match(Kw("modes")))
            {
                var result = new LobbyModesMatch();
                Match("{"); // Start modes section.
                while (LobbyModes(ruleset, result)); // Match the mode settings.
                Match("}"); // End modes section.
                ruleset.Modes = result.GetModesRoot();
            }

            // Heroes
            if (Match(Kw("heroes")))
            {
                ruleset.Heroes = new HeroesRoot();
                Match("{"); // Start heroes section.

                // Match the hero settings.
                while (HeroSettingsGroup(ruleset)) ;

                Match("}"); // End heroes section.
            }

            // Custom workshop settings
            if (Match(Kw("workshop")))
            {
                ruleset.Workshop = new WorkshopValuePair();
                Match("{"); // Start workshop section.

                // Match settings.
                while (!Match("}"))
                {
                    string identifier = CustomSettingName();
                    Match(":");

                    object value = "?";

                    // Boolean: On
                    if (Match(Kw("On")))
                        value = true;
                    // Boolean: Off
                    else if (Match(Kw("Off")))
                        value = false;
                    // Number
                    else if (Double(out double num))
                        value = num;
                    // Combo
                    else if (Match("["))
                    {
                        Double(out double comboIndex);
                        value = comboIndex;
                        Match("]");
                    }
                    // Match hero names.
                    else if (MatchHeroNames(out var hero))
                        value = hero.CollectionName;

                    // Add the custom setting.
                    ruleset.Workshop.Add(identifier, value);
                }
            }

            Match("}"); // End settings section.
            LobbySettings = ruleset;
            return true;
        }

        bool LobbyModes(Ruleset ruleset, LobbyModesMatch result)
        {
            // Match general
            if (Match(Kw("General")))
            {
                var generalSettings = new List<SettingPair>();

                Match("{"); // Start general settings section.
                GroupSettings(generalSettings, ModeSettingCollection.AllModeSettings.First(modeSettings => modeSettings.CollectionName == "All").ToArray()); // Match settings.
                Match("}"); // End general settings section.

                result.GeneralSettings = generalSettings.ToArray();
                return true;
            }

            // Disabled
            bool disabled = Match(Kw("disabled"));

            foreach (var mode in ModeSettingCollection.AllModeSettings)
                // Match the mode name.
                if (Match(Kw(mode.CollectionName)))
                {
                    var modeData = new ModeSettings(mode.CollectionName);

                    // The list of settings for the mode.
                    var settings = new List<SettingPair>();

                    Match("{"); // Start specific mode settings section.
                    // Match the value pairs.
                    GroupSettings(settings, mode.ToArray(), () =>
                    {
                        bool matchingEnabledMaps; // Determines if the map group is matching enabled or disabled maps.
                                                  // Match enabled maps
                        if (Match(Kw("enabled maps"))) matchingEnabledMaps = true;
                        // Match disabled maps
                        else if (Match(Kw("disabled maps"))) matchingEnabledMaps = false;
                        // End
                        else return false;

                        Match("{"); // Start map section.

                        List<string> maps = new List<string>(); // Matched maps.

                        // Match map names.
                        bool matched = true;
                        while (matched)
                        {
                            matched = false;
                            // Only match maps related to the current mode.
                            foreach (var map in LobbyMap.AllMaps.Where(m => m.GameModes.Any(mapMode => mapMode.ToLower() == mode.CollectionName.ToLower())).OrderByDescending(map => map.GetWorkshopName().Length))
                                // Match the map.
                                if (Match(Kw(map.GetWorkshopName()), false))
                                {
                                    // Add the map.
                                    maps.Add(map.Name);

                                    // Indicate that a map was matched in this iteration.
                                    matched = true;
                                    break;
                                }
                        }

                        Match("}"); // End map section.

                        // Add the maps to the mode's settings.
                        if (matchingEnabledMaps) modeData.EnabledMaps = maps.ToArray();
                        else modeData.DisabledMaps = maps.ToArray();

                        return true;
                    });
                    Match("}"); // End specific mode settings section.

                    modeData.Settings = settings.ToArray(); // Set mode settings.
                    modeData.Enabled = !disabled; // Set enabled status.
                    result.ModeSettings.Add(modeData);
                    return true;
                }
            return false;
        }

        bool HeroSettingsGroup(Ruleset ruleset)
        {
            // Matched settings will be added to this list.
            HeroTeamGroup teamGroup = new HeroTeamGroup();

            // Match hero settings group name.
            if (Match(Kw("General"))) ruleset.Heroes.General = teamGroup;   // General
            else if (Match(Kw("Team 1"))) ruleset.Heroes.Team1 = teamGroup; // Team 1
            else if (Match(Kw("Team 2"))) ruleset.Heroes.Team2 = teamGroup; // Team 2
            else return false;

            Match("{"); // Start hero settings section.

            var general = new List<SettingPair>(); // General settings.
            var heroGroups = new List<HeroSettingGroup>(); // Hero settings.

            // Match general settings.
            GroupSettings(general, HeroSettingCollection.AllHeroSettings.First(hero => hero.CollectionName == "General").ToArray(), () =>
            {
                // Match hero names.
                if (MatchHeroNames(out var hero))
                {
                    var heroSettingsGroup = new HeroSettingGroup(hero.CollectionName);
                    var heroSettings = new List<SettingPair>();

                    Match("{"); // Start specific hero settings section.
                    GroupSettings(heroSettings, hero.ToArray()); // Match settings.
                    Match("}"); // End specific hero settings section.

                    heroSettingsGroup.Settings = new SettingPairCollection(heroSettings); // Copy the hero settings to the hero group.
                    heroGroups.Add(heroSettingsGroup); // Add the hero group to the list.
                    return true;
                }
                else
                {
                    bool enabledHeroes; // Determines if the hero group is matching enabled or disabled heroes.
                    // Enabled heroes
                    if (Match(Kw("enabled heroes"))) enabledHeroes = true;
                    // Disabled heroes
                    else if (Match(Kw("disabled heroes"))) enabledHeroes = false;
                    // No heroes
                    else return false;

                    var heroes = new List<string>(); // The list of heroes in the collection.

                    Match("{"); // Start the enabled heroes section.
                    while (MatchHero(out string heroName)) heroes.Add(heroName); // Match heroes.
                    Match("}"); // End the enabled heroes section.

                    // Apply the hero list.
                    if (enabledHeroes) teamGroup.EnabledHeroes = heroes.ToArray();
                    else teamGroup.DisabledHeroes = heroes.ToArray();

                    // Done
                    return true;
                }
            });

            heroGroups.Add(new HeroSettingGroup("General", new SettingPairCollection(general))); // Add the general settings.
            teamGroup.Heroes = heroGroups.ToArray(); // Set the heroes to the team group.

            Match("}"); // End hero settings section.
            return true;
        }

        bool MatchHeroNames(out HeroSettingCollection collection)
        {
            foreach (var hero in HeroSettingCollection.AllHeroSettings.Where(heroSettings => heroSettings.CollectionName != "General"))
                if (Match(Kw(hero.CollectionName), false))
                {
                    collection = hero;
                    return true;
                }
            collection = null;
            return false;
        }

        bool MatchHero(out string heroName)
        {
            // Iterate through all hero names.
            foreach (var hero in HeroSettingCollection.AllHeroSettings)
                // If a hero name is matched, return true.
                if (Match(Kw(hero.CollectionName), false))
                {
                    heroName = hero.CollectionName;
                    return true;
                }
            // Otherwise, return false.
            heroName = null;
            return false;
        }

        void GroupSettings(IList<SettingPair> collection, LobbySetting[] settings, Func<Boolean> onInterupt = null)
        {
            var orderedSettings = settings.OrderByDescending(s => s.Name.Length); // Order the settings so longer names are matched first.

            bool matched = true;
            while (matched)
            {
                matched = false;

                // Test hook.
                if (onInterupt != null && onInterupt.Invoke())
                {
                    // If the hook handled the match, continue.
                    matched = true;
                    continue;
                }

                foreach (var lobbySetting in orderedSettings)
                {
                    // Match the setting name.
                    if (MatchLobbySetting(lobbySetting, out var setting))
                    {
                        collection.Add(setting);

                        // Indicate that a setting was matched.
                        matched = true;
                        break;
                    }
                }
            }
        }

        bool MatchLobbySetting(LobbySetting setting, out SettingPair result)
        {
            // Match the setting name.
            if (Match(Kw(setting.Workshop), false))
            {
                Match(":"); // Match the value seperator.
                setting.Match(this, out object value); // Match the setting value.

                // Add the setting.
                result = new SettingPair(setting.Name, value);
                return true;
            }
            result = null;
            return false;
        }
    }

    class LobbyModesMatch
    {
        public SettingPair[] GeneralSettings { get; set; }
        public List<ModeSettings> ModeSettings { get; } = new List<ModeSettings>();

        public ModesRoot GetModesRoot() => new ModesRoot(ModeSettings) { GeneralSettings = new SettingPairCollection(GeneralSettings) };
    }
}