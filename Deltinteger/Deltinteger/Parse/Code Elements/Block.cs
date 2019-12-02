using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class BlockAction : CodeAction
    {
        public IStatement[] Statements { get; }

        public BlockAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.BlockContext blockContext)
        {
            if (blockContext.statement() == null) Statements = new IStatement[0];
            else
            {
                Statements = new IStatement[blockContext.statement().Length];
                for (int i = 0; i < Statements.Length; i++)
                    Statements[i] = GetStatement(script, translateInfo, scope, blockContext.statement(i));
            }
        }
    }
}