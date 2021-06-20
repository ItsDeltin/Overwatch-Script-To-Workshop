using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class BlockAction : IStatement, IBlockContainer
    {
        public IStatement[] Statements { get; }
        public Scope BlockScope { get; }
        public string EndComment {get; private set;}


        public BlockAction(ParseInfo parseInfo, Scope scope, Block blockContext)
        {
            BlockScope = scope.Child();

            Statements = new IStatement[blockContext.Statements.Count];
            for (int i = 0; i < Statements.Length; i++)
                Statements[i] = parseInfo.GetStatement(BlockScope, blockContext.Statements[i]);

            parseInfo.Script.AddCompletionRange(new CompletionRange(parseInfo.TranslateInfo, BlockScope, blockContext.Range, CompletionRangeKind.Catch));
            EndComment = blockContext.EndComment?.GetContents();
        }

        public BlockAction(IStatement[] statements)
        {
            Statements = statements;
        }


        public void Translate(ActionSet actionSet)
        {
            actionSet = actionSet.ContainVariableAssigner();

            foreach (var statement in Statements)
                statement.Translate(actionSet);

            if (!Statements.Any(s => s is ReturnAction))
                BlockScope?.EndScope(actionSet, false);
        }

        public PathInfo[] GetPaths() => new PathInfo[] { new PathInfo(this, null, true) };
    }
}
