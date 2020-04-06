using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ContinueAction : IStatement
    {
        private IContinueContainer Loop { get; }

        public ContinueAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the continue statement is not in a loop.
            if (parseInfo.ContinueHandler == null) parseInfo.Script.Diagnostics.Error("No loop to continue in.", range);
            Loop = parseInfo.ContinueHandler;
        }

        public void Translate(ActionSet actionSet)
        {
            SkipStartMarker continuer = new SkipStartMarker(actionSet);
            actionSet.AddAction(continuer);
            Loop.AddContinue(continuer);
        }
    }

    public class BreakAction : IStatement
    {
        private IBreakContainer BreakContainer { get; }

        public BreakAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the break statement is not in a loop.
            if (parseInfo.BreakHandler == null) parseInfo.Script.Diagnostics.Error("No loop to break out of.", range);
            BreakContainer = parseInfo.BreakHandler;
        }

        public void Translate(ActionSet actionSet)
        {
            SkipStartMarker breaker = new SkipStartMarker(actionSet);
            actionSet.AddAction(breaker);
            BreakContainer.AddBreak(breaker);
        }
    }
}