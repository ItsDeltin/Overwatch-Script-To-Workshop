using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class EventInfo
    {
        public static readonly (string, RuleEvent)[] PlayerEventNames = new (string, RuleEvent)[] {
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
            ("Player Received Knockback", RuleEvent.PlayerReceivedKnockback)
        };
        public static readonly (string, PlayerSelector)[] PlayerTypeNames = new (string, PlayerSelector)[] {
            ("All", PlayerSelector.All),
            ("Ana", PlayerSelector.Ana),
            ("Ashe", PlayerSelector.Ashe),
            ("Baptiste", PlayerSelector.Baptiste),
            ("Bastion", PlayerSelector.Bastion),
            ("Brigitte", PlayerSelector.Brigitte),
            ("Doomfist", PlayerSelector.Doomfist),
            ("D.Va", PlayerSelector.Dva),
            ("Echo", PlayerSelector.Echo),
            ("Genji", PlayerSelector.Genji),
            ("Hanzo", PlayerSelector.Hanzo),
            ("Junkrat", PlayerSelector.Junkrat),
            ("Lúcio", PlayerSelector.Lucio),
            ("Cassidy", PlayerSelector.Cassidy),
            ("Mei", PlayerSelector.Mei),
            ("Mercy", PlayerSelector.Mercy),
            ("Moira", PlayerSelector.Moira),
            ("Orisa", PlayerSelector.Orisa),
            ("Pharah", PlayerSelector.Pharah),
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
            ("Symmetra", PlayerSelector.Symmetra),
            ("Torbjörn", PlayerSelector.Torbjorn),
            ("Tracer", PlayerSelector.Tracer),
            ("Widowmaker", PlayerSelector.Widowmaker),
            ("Winston", PlayerSelector.Winston),
            ("Wrecking Ball", PlayerSelector.WreckingBall),
            ("Zarya", PlayerSelector.Zarya),
            ("Zenyatta", PlayerSelector.Zenyatta)
        };
        public RuleEvent Event { get; }
        public PlayerSelector Player { get; }
        public Team Team { get; }
        public string SubroutineName { get; }

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
