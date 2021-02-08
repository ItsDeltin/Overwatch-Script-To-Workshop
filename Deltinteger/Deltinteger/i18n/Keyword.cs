using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using static Deltin.Deltinteger.Lobby.LobbyConstants;

namespace Deltin.Deltinteger.I18n
{
    public class Keyword
    {
        public const string KEYWORD_VARIABLES = "keyword.variables";
        public const string KEYWORD_GLOBAL = "keyword.global";
        public const string KEYWORD_PLAYER = "keyword.player";
        public const string KEYWORD_RULE = "keyword.rule";
        public const string KEYWORD_EVENT = "keyword.event";
        public const string KEYWORD_CONDITIONS = "keyword.conditions";
        public const string KEYWORD_ACTIONS = "keyword.actions";
        public const string KEYWORD_RULE_DISABLED = "keyword.disabled";
        public const string KEYWORD_SUBROUTINES = "keyword.subroutines";
        public const string KEYWORD_SETTINGS = "keyword.settings";
        public const string KEYWORD_LOBBY = "keyword.lobby";
        public const string KEYWORD_MODES = "keyword.modes";
        public const string KEYWORD_HEROES = "keyword.heroes";
        public const string KEYWORD_GENERAL = "keyword.General";
        public const string KEYWORD_TEAM1 = "keyword.Team 1";
        public const string KEYWORD_TEAM2 = "keyword.Team 2";
        public const string KEYWORD_ENABLED_MAPS = "keyword.enabled maps";
        public const string KEYWORD_DISABLED_MAPS = "keyword.disabled maps";
        public const string KEYWORD_ENABLED_HEROES = "keyword.enabled heroes";
        public const string KEYWORD_DISABLED_HEROES = "keyword.disabled heroes";
        public const string KEYWORD_ENABLED = "keyword.Enabled";
        public const string KEYWORD_DISABLED = "keyword.Disabled";
        public const string KEYWORD_ON = "keyword.On";
        public const string KEYWORD_OFF = "keyword.Off";
        public const string KEYWORD_YES = "keyword.Yes";
        public const string KEYWORD_NO = "keyword.No";
        public const string KEYWORD_MAIN = "keyword.main";
        public const string KEYWORD_DESCRIPTION = "keyword.Description";
        public const string KEYWORD_WORKSHOP = "keyword.workshop";
        public const string KEYWORD_I18N_COOLDOWN_TIME = "keyword.I18N_COOLDOWN_TIME";
        public const string KEYWORD_I18N_RECHARGE_RATE = "keyword.I18N_RECHARGE_RATE";
        public const string KEYWORD_I18N_MAXIMUM_TIME = "keyword.I18N_MAXIMUM_TIME";
        public const string KEYWORD_I18N_ULTIMATE_ABILITY = "keyword.I18N_ULTIMATE_ABILITY";
        public const string KEYWORD_I18N_ULTIMATE_GENERATION = "keyword.I18N_ULTIMATE_GENERATION";
        public const string KEYWORD_I18N_ULTIMATE_GENERATION_PASSIVE = "keyword.I18N_ULTIMATE_GENERATION_PASSIVE";
        public const string KEYWORD_I18N_ULTIMATE_GENERATION_COMBAT = "keyword.I18N_ULTIMATE_GENERATION_COMBAT";
        public const string KEYWORD_I18N_MAX_TEAM_PLAYERS = "keyword.I18N_MAX_TEAM_PLAYERS";

        public string ID { get; }
        public string Name { get; }

        public Keyword(string pathIdentifier, string name)
        {
            ID = pathIdentifier ?? throw new ArgumentNullException(nameof(pathIdentifier));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString() => ID;

        public static implicit operator Keyword((string, string) pair) => new Keyword(pair.Item1, pair.Item2);

        public static Keyword[] GetKeywords()
        {
            var keywords = new List<Keyword>() {
                (KEYWORD_VARIABLES, "variables"),
                (KEYWORD_GLOBAL, "global"),
                (KEYWORD_PLAYER, "player"),
                (KEYWORD_RULE, "rule"),
                (KEYWORD_EVENT, "event"),
                (KEYWORD_CONDITIONS, "conditions"),
                (KEYWORD_ACTIONS, "actions"),
                (KEYWORD_RULE_DISABLED, "disabled"),
                (KEYWORD_SUBROUTINES, "subroutines"),

                // Lobby settings
                (KEYWORD_SETTINGS, "settings"),
                (KEYWORD_LOBBY, "lobby"),
                (KEYWORD_MODES, "modes"),
                (KEYWORD_HEROES, "heroes"),
                (KEYWORD_GENERAL, "General"),
                (KEYWORD_TEAM1, "Team 1"),
                (KEYWORD_TEAM2, "Team 2"),
                (KEYWORD_ENABLED_MAPS, "enabled maps"),
                (KEYWORD_DISABLED_MAPS, "disabled maps"),
                (KEYWORD_ENABLED_HEROES, "enabled heroes"),
                (KEYWORD_DISABLED_HEROES, "disabled heroes"),
                (KEYWORD_ENABLED, "Enabled"),
                (KEYWORD_DISABLED, "Disabled"),
                (KEYWORD_ON, "On"),
                (KEYWORD_OFF, "Off"),
                (KEYWORD_YES, "Yes"),
                (KEYWORD_NO, "No"),
                (KEYWORD_MAIN, "main"),
                (KEYWORD_DESCRIPTION, "Description"),
                (KEYWORD_WORKSHOP, "workshop"),
                // Formatted lobby settings
                (KEYWORD_I18N_COOLDOWN_TIME, I18N_COOLDOWN_TIME),
                (KEYWORD_I18N_RECHARGE_RATE, I18N_RECHARGE_RATE),
                (KEYWORD_I18N_MAXIMUM_TIME, I18N_MAXIMUM_TIME),
                (KEYWORD_I18N_ULTIMATE_ABILITY, I18N_ULTIMATE_ABILITY),
                (KEYWORD_I18N_ULTIMATE_GENERATION, I18N_ULTIMATE_GENERATION),
                (KEYWORD_I18N_ULTIMATE_GENERATION_PASSIVE, I18N_ULTIMATE_GENERATION_PASSIVE),
                (KEYWORD_I18N_ULTIMATE_GENERATION_COMBAT, I18N_ULTIMATE_GENERATION_COMBAT),
                (KEYWORD_I18N_MAX_TEAM_PLAYERS, I18N_MAX_TEAM_PLAYERS)
            };

            // Add actions
            keywords.AddRange(ElementRoot.Instance.Actions.Select(e => new Keyword(e.GetI18nIdentifier(), e.Name)));

            // Add values
            keywords.AddRange(ElementRoot.Instance.Values.Select(e => new Keyword(e.GetI18nIdentifier(), e.Name)));

            // Add enums
            foreach(var enumData in ElementRoot.Instance.Enumerators)
                foreach (var member in enumData.Members)
                    keywords.Add((member.I18nIdentifier(), member.I18n));
            
            // Get lobby keywords.
            keywords.AddRange(KeywordsFromSettingsCollection(Ruleset.LobbySettings));

            // Get hero keywords.
            foreach (var heroCollection in HeroSettingCollection.AllHeroSettings)
            {
                keywords.Add(($"hero.{heroCollection.HeroName}", heroCollection.HeroName));
                keywords.AddRange(KeywordsFromSettingsCollection(heroCollection));
            }
            
            // Get mode keywords.
            foreach (var modeCollection in ModeSettingCollection.AllModeSettings)
            {
                keywords.Add(($"mode.{modeCollection.ModeName}", modeCollection.ModeName));
                keywords.AddRange(KeywordsFromSettingsCollection(modeCollection));
            }

            // Get map keywords.
            foreach (var map in LobbyMap.AllMaps)
                keywords.Add(($"map.{map.Name}", map.Name));
            
            // Distinct by ID.
            keywords = keywords.GroupBy(k => k.ID).Select(g => g.First()).ToList();
            return keywords.ToArray();
        }

        static IEnumerable<Keyword> KeywordsFromSettingsCollection(IEnumerable<LobbySetting> settings)
        {
            var keywords = new List<Keyword>();

            // Get hero keywords.
            foreach (var setting in settings)
            {
                keywords.Add(setting.TitleResolver.GetKeyword());
                keywords.AddRange(setting.AdditionalKeywords());
            }
            
            return keywords;
        }
    }
}