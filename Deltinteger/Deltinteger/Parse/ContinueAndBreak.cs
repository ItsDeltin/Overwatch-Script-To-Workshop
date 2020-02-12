using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ContinueAction : IStatement
    {
        private LoopAction Loop { get; }

        public ContinueAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the continue statement is not in a loop.
            if (parseInfo.Loop == null) parseInfo.Script.Diagnostics.Error("No loop to continue in.", range);
            Loop = parseInfo.Loop;
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
        private LoopAction Loop { get; }

        public BreakAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the break statement is not in a loop.
            if (parseInfo.Loop == null) parseInfo.Script.Diagnostics.Error("No loop to break out of.", range);
            Loop = parseInfo.Loop;
        }

        public void Translate(ActionSet actionSet)
        {
            SkipStartMarker breaker = new SkipStartMarker(actionSet);
            actionSet.AddAction(breaker);
            Loop.AddBreak(breaker);
        }
    }
}