using System;
using DS.Analysis.Statements;
using DS.Analysis.Scopes;

namespace DS.Analysis
{
    class BlockAction : IDisposable
    {
        public Statement[] Statements { get; }
        readonly ScopeSource scopeSource;

        public BlockAction(Statement[] statements, ScopeSource scopeSource)
        {
            Statements = statements;
            this.scopeSource = scopeSource;
        }


        public void Dispose()
        {
            foreach (var statement in Statements)
                statement.Dispose();
        }
    }
}