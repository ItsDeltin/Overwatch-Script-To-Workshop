using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ContinueAction : IStatement
    {
        private IContinueContainer Loop { get; }
        private string Comment;

        public ContinueAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the continue statement is not in a loop.
            if (parseInfo.ContinueHandler == null) parseInfo.Script.Diagnostics.Error("No loop to continue in.", range);
            Loop = parseInfo.ContinueHandler;
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        public void Translate(ActionSet actionSet)
        {
            Loop.AddContinue(actionSet, Comment);
        }
    }

    public class BreakAction : IStatement
    {
        private IBreakContainer BreakContainer { get; }
        private string Comment;

        public BreakAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the break statement is not in a loop.
            if (parseInfo.BreakHandler == null) parseInfo.Script.Diagnostics.Error("No loop to break out of.", range);
            BreakContainer = parseInfo.BreakHandler;
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        public void Translate(ActionSet actionSet)
        {
            BreakContainer.AddBreak(actionSet, Comment);
        }
    }
}