using System.Linq;
using Deltin.Deltinteger.Elements;

#nullable enable

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class EventInfo
    {
        public static readonly (string, RuleEvent)[] PlayerEventNames = new (string, RuleEvent)[] {
            ("Ongoing - Global", RuleEvent.OngoingGlobal),
            ("Ongoing - Each Player", RuleEvent.OngoingPlayer),
            ("Player Earned Elimination", RuleEvent.OnElimination),
            ("Player Dealt Final Blow", RuleEvent.OnFinalBlow),
            ("Player Dealt Damage", RuleEvent.OnDamageDealt),
            ("Player Took Damage", RuleEvent.OnDamageTaken),
            ("Player Died", RuleEvent.OnDeath),
            ("Player Dealt Healing", RuleEvent.OnHealingDealt),
            ("Player Received Healing", RuleEvent.OnHealingTaken),
            ("Player Joined Match", RuleEvent.OnPlayerJoin),
            ("Player Left Match", RuleEvent.OnPlayerLeave),
            ("Player Dealt Knockback", RuleEvent.PlayerDealtKnockback),
            ("Player Received Knockback", RuleEvent.PlayerReceivedKnockback),
            ("Subroutine", RuleEvent.Subroutine)
        };
        public static readonly (string, Team)[] PlayerTeamNames = new (string, Team)[] {
            ("All", Team.All),
            ("Team1", Team.Team1),
            ("Team2", Team.Team2),
            ("Team 1", Team.Team1),
            ("Team 2", Team.Team2),
        };
        public static readonly (string, PlayerSelector)[] PlayerTypeNames = new (string, PlayerSelector)[] {
            ("All", PlayerSelector.All),
            ("Ana", PlayerSelector.Ana),
            ("Ashe", PlayerSelector.Ashe),
            ("Baptiste", PlayerSelector.Baptiste),
            ("Bastion", PlayerSelector.Bastion),
            ("Brigitte", PlayerSelector.Brigitte),
            ("Cassidy", PlayerSelector.Cassidy),
            ("Doomfist", PlayerSelector.Doomfist),
            ("D.va", PlayerSelector.Dva),
            ("Echo", PlayerSelector.Echo),
            ("Genji", PlayerSelector.Genji),
            ("Hanzo", PlayerSelector.Hanzo),
            ("Junker Queen", PlayerSelector.JunkerQueen),
            ("Junkrat", PlayerSelector.Junkrat),
            ("Kiriko", PlayerSelector.Kiriko),
            ("Lifeweaver", PlayerSelector.Lifeweaver),
            ("Illari", PlayerSelector.Illari),
            ("Lúcio", PlayerSelector.Lucio),
            ("Mauga", PlayerSelector.Mauga),
            ("McCree", PlayerSelector.Cassidy),
            ("Mei", PlayerSelector.Mei),
            ("Mercy", PlayerSelector.Mercy),
            ("Moira", PlayerSelector.Moira),
            ("Orisa", PlayerSelector.Orisa),
            ("Pharah", PlayerSelector.Pharah),
            ("Ramattra", PlayerSelector.Ramattra),
            ("Reaper", PlayerSelector.Reaper),
            ("Reinhardt", PlayerSelector.Reinhardt),
            ("Roadhog", PlayerSelector.Roadhog),
            ("Sigma", PlayerSelector.Sigma),
            ("Slot 0", PlayerSelector.Slot0),
            ("Slot 1", PlayerSelector.Slot1),
            ("Slot 2", PlayerSelector.Slot2),
            ("Slot 3", PlayerSelector.Slot3),
            ("Slot 4", PlayerSelector.Slot4),
            ("Slot 5", PlayerSelector.Slot5),
            ("Slot 6", PlayerSelector.Slot6),
            ("Slot 7", PlayerSelector.Slot7),
            ("Slot 8", PlayerSelector.Slot8),
            ("Slot 9", PlayerSelector.Slot9),
            ("Slot 10", PlayerSelector.Slot10),
            ("Slot 11", PlayerSelector.Slot11),
            ("Soldier: 76", PlayerSelector.Soldier76),
            ("Sombra", PlayerSelector.Sombra),
            ("Sojourn", PlayerSelector.Sojourn),
            ("Symmetra", PlayerSelector.Symmetra),
            ("Torbjörn", PlayerSelector.Torbjorn),
            ("Tracer", PlayerSelector.Tracer),
            ("Widowmaker", PlayerSelector.Widowmaker),
            ("Winston", PlayerSelector.Winston),
            ("Wrecking Ball", PlayerSelector.WreckingBall),
            ("Zarya", PlayerSelector.Zarya),
            ("Zenyatta", PlayerSelector.Zenyatta)
        };

        static T? Search<T>((string, T)[] collection, string enUsName)
        {
            return collection.FirstOrDefault(pair => pair.Item1 == enUsName).Item2;
        }

        public static RuleEvent? EventFromString(string enUsEventName) => Search(PlayerEventNames, enUsEventName);
        public static Team? TeamFromString(string enUsTeamName) => Search(PlayerTeamNames, enUsTeamName);
        public static PlayerSelector? PlayerFromString(string enUsPlayerName) => Search(PlayerTypeNames, enUsPlayerName);

        public RuleEvent Event { get; } = default;
        public PlayerSelector Player { get; } = default;
        public Team Team { get; } = default;
        public string? SubroutineName { get; }

        public EventInfo()
        {
        }
        public EventInfo(string subroutineName)
        {
            Event = RuleEvent.Subroutine;
            SubroutineName = subroutineName;
        }
        public EventInfo(RuleEvent ruleEvent, PlayerSelector player, Team team)
        {
            Event = ruleEvent;
            Player = player;
            Team = team;
        }
    }
}
