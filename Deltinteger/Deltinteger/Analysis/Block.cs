using System;
using DS.Analysis.Statements;
using DS.Analysis.Scopes;

namespace DS.Analysis
{
    class BlockAction : IDisposable
    {
        public IDisposableStatement[] Statements { get; }
        readonly ScopeSource scopeSource;

        public BlockAction(IDisposableStatement[] statements, ScopeSource scopeSource)
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