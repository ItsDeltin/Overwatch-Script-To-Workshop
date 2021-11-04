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

        public void GetMeta(ContextInfo contextInfo)
        {
            contextInfo = contextInfo.SetScope(contextInfo.Scope.CreateChild(scopeSource));

            foreach (var statement in Statements)
            {
                statement.GetMeta(contextInfo);

                var continueWithScope = statement.ProceedWithScope();
                if (continueWithScope != null)
                    contextInfo = contextInfo.SetScope(continueWithScope);
            }
        }

        public void GetContent()
        {
            foreach (var statement in Statements)
                statement.GetContent();
        }

        public void Dispose()
        {
            foreach (var statement in Statements)
                statement.Dispose();
        }
    }
}