using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class ContinueAction : IStatement
    {
        private string Comment;

        public ContinueAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the continue statement is not in a loop.
            if (!parseInfo.ContinuesAllowed)
                parseInfo.Script.Diagnostics.Error("No loop to continue in.", range);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        public void Translate(ActionSet actionSet)
        {
            actionSet.ContinueHandler.AddContinue(Comment);
        }
    }

    public class BreakAction : IStatement
    {
        private string Comment;

        public BreakAction(ParseInfo parseInfo, DocRange range)
        {
            // Syntax error if the break statement is not in a loop.
            if (!parseInfo.BreaksAllowed)
                parseInfo.Script.Diagnostics.Error("No loop to break out of.", range);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        public void Translate(ActionSet actionSet)
        {
            actionSet.BreakHandler.AddBreak(Comment);
        }
    }
}