using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    static class ExecuteSubroutine
    {
        public static void Execute(ActionSet actionSet, Subroutine subroutine, CallParallel callParallel = CallParallel.NoParallel)
        {
            switch (callParallel)
            {
                case CallParallel.NoParallel:
                    actionSet.AddAction(Element.CallSubroutine(subroutine));
                    break;

                case CallParallel.AlreadyRunning_DoNothing:
                    actionSet.AddAction(Element.StartRule(subroutine, false));
                    break;

                case CallParallel.AlreadyRunning_RestartRule:
                    actionSet.AddAction(Element.StartRule(subroutine, true));
                    break;
            }
        }
    }
}