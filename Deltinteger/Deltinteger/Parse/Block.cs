#nullable enable

using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class BlockAction : IStatement
    {
        public IStatement[] Statements { get; }
        public string? EndComment { get; private set; }

        readonly ParseInfo? parseInfo;
        readonly Block? context;

        public BlockAction(ParseInfo parseInfo, Scope scope, Block blockContext)
        {
            this.parseInfo = parseInfo;
            context = blockContext;
            var blockScope = scope.Child();

            Statements = new IStatement[blockContext.Statements.Count];
            for (int i = 0; i < Statements.Length; i++)
                Statements[i] = parseInfo.GetStatement(blockScope, blockContext.Statements[i]);

            parseInfo.Script.AddCompletionRange(new CompletionRange(parseInfo.TranslateInfo, blockScope, blockContext.Range, CompletionRangeKind.Catch));
            EndComment = blockContext.EndComment?.GetContents();
        }

        public BlockAction(IStatement[] statements)
        {
            Statements = statements;
        }


        public void Translate(ActionSet actionSet)
        {
            actionSet = actionSet.ContainVariableAssigner().AddRecursiveVariableTracker();

            for (int i = 0; i < Statements.Length; i++)
            {
                var statementActionSet = actionSet;

                // If context is available, apply the location to the ActionSet used to compile the next statement.
                // Null check is required because switch nodes uses BlockAction to represent statements between cases for some reason (bad design).
                // See: Issue #486
                // I have opted to just deal with it here rather than touching switch code.
                // `SetLocation` is only used to improve certain error messages and is not important to compiling.
                if (context is not null && parseInfo is not null)
                    statementActionSet = statementActionSet.SetLocation(parseInfo.Script, context.Statements[i].Range);

                statementActionSet.CompileStatement(Statements[i]);
            }

            actionSet.RecursiveVariableTracker.PopLocal();
        }

        public PathInfo[] GetPaths() => new PathInfo[] { new PathInfo(this, null, true) };
    }
}
