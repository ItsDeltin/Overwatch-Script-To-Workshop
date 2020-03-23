using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class BlockAction : IStatement, IBlockContainer
    {
        public IStatement[] Statements { get; }
        public Scope BlockScope { get; }

        public BlockAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.BlockContext blockContext)
        {
            BlockScope = scope.Child();

            if (blockContext.statement() == null) Statements = new IStatement[0];
            else
            {
                Statements = new IStatement[blockContext.statement().Length];
                for (int i = 0; i < Statements.Length; i++)
                    Statements[i] = DeltinScript.GetStatement(parseInfo, BlockScope, blockContext.statement(i));
            }

            if (blockContext.BLOCK_START() != null && blockContext.BLOCK_END() != null)
                parseInfo.Script.AddCompletionRange(new CompletionRange(BlockScope, DocRange.GetRange(blockContext.BLOCK_START(), blockContext.BLOCK_END()), CompletionRangeKind.Catch));
        }

        public BlockAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.S_blockContext blockContext)
        {
            BlockScope = scope.Child();

            if (blockContext.statement() == null) Statements = new IStatement[0];
            else
            {
                Statements = new IStatement[blockContext.statement().Length];
                for (int i = 0; i < Statements.Length; i++)
                    Statements[i] = DeltinScript.GetStatement(parseInfo, BlockScope, blockContext.statement(i));
            }

            parseInfo.Script.AddCompletionRange(new CompletionRange(BlockScope, DocRange.GetRange(blockContext.BLOCK_START(), blockContext.BLOCK_END()), CompletionRangeKind.Catch));
        }

        public BlockAction(IStatement[] statements)
        {
            Statements = statements;
        }

        public void Translate(ActionSet actionSet)
        {
            foreach (var statement in Statements)
                statement.Translate(actionSet);
            
            BlockScope?.EndScope(actionSet);
        }

        public PathInfo[] GetPaths() => new PathInfo[] { new PathInfo(this, null, true) };
    }
}