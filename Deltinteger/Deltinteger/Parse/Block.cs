using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class BlockAction : IStatement, IBlockContainer
    {
        public IStatement[] Statements { get; }

        public BlockAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.BlockContext blockContext)
        {
            Scope blockScope = scope.Child();

            if (blockContext.statement() == null) Statements = new IStatement[0];
            else
            {
                Statements = new IStatement[blockContext.statement().Length];
                for (int i = 0; i < Statements.Length; i++)
                    Statements[i] = DeltinScript.GetStatement(parseInfo, blockScope, blockContext.statement(i));
            }

            if (blockContext.BLOCK_START() != null && blockContext.BLOCK_END() != null)
                parseInfo.Script.AddCompletionRange(new CompletionRange(blockScope, DocRange.GetRange(blockContext.BLOCK_START(), blockContext.BLOCK_END()), CompletionRangeKind.Catch));
        }

        public BlockAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.S_blockContext blockContext)
        {
            Scope blockScope = scope.Child();

            if (blockContext.statement() == null) Statements = new IStatement[0];
            else
            {
                Statements = new IStatement[blockContext.statement().Length];
                for (int i = 0; i < Statements.Length; i++)
                    Statements[i] = DeltinScript.GetStatement(parseInfo, blockScope, blockContext.statement(i));
            }

            parseInfo.Script.AddCompletionRange(new CompletionRange(blockScope, DocRange.GetRange(blockContext.BLOCK_START(), blockContext.BLOCK_END()), CompletionRangeKind.Catch));
        }

        public BlockAction(IStatement[] statements)
        {
            Statements = statements;
        }

        public void Translate(ActionSet actionSet)
        {
            foreach (var statement in Statements)
                statement.Translate(actionSet);
        }

        public PathInfo[] GetPaths() => new PathInfo[] { new PathInfo(this, null, true) };
    }
}