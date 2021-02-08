using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class TTEWorkshop
    {
        public WorkshopVariable[] Variables { get; }
        public Subroutine[] Subroutines { get; }
        public TTERule[] Rules { get; }
        public Ruleset LobbySettings { get; }
        public ITTEAction[] Actions { get; }
        public TTECondition[] Conditions { get; }

        public TTEWorkshop(WorkshopVariable[] variables, Subroutine[] subroutines, TTERule[] rules, Ruleset settings)
        {
            Variables = variables;
            Subroutines = subroutines;
            Rules = rules;
            LobbySettings = settings;
        }
        public TTEWorkshop(WorkshopVariable[] variables, Subroutine[] subroutines, ITTEAction[] actions)
        {
            Variables = variables;
            Subroutines = subroutines;
            Actions = actions;
        }
        public TTEWorkshop(WorkshopVariable[] variables, Subroutine[] subroutines, TTECondition[] conditions)
        {
            Variables = variables;
            Subroutines = subroutines;
            Conditions = conditions;
        }
    }
}