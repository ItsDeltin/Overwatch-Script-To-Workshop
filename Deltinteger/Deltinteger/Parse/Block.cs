using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class BlockAction
    {
        public IStatement[] Statements { get; }

        public BlockAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.BlockContext blockContext)
        {
            if (blockContext.statement() == null) Statements = new IStatement[0];
            else
            {
                Statements = new IStatement[blockContext.statement().Length];
                for (int i = 0; i < Statements.Length; i++)
                    Statements[i] = DeltinScript.GetStatement(script, translateInfo, scope, blockContext.statement(i));
            }

            if (blockContext.BLOCK_START() != null && blockContext.BLOCK_END() != null)
                script.AddCompletionRange(new CompletionRange(scope, DocRange.GetRange(blockContext.BLOCK_START(), blockContext.BLOCK_END())));
        }

        public void Translate(ActionSet actionSet)
        {
            foreach (var statement in Statements)
                statement.Translate(actionSet);
        }
    }
}