using System;
using DS.Analysis.Statements;
using DS.Analysis.Scopes;
using DS.Analysis.Structure;
using Deltin.Deltinteger.Compiler.SyntaxTree;

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
                statement.GetMeta(contextInfo);
        }

        public void GetContent(ContextInfo contextInfo)
        {
            contextInfo = contextInfo.SetScope(contextInfo.Scope.CreateChild(scopeSource));

            foreach (var statement in Statements)
                statement.GetContent(contextInfo);
        }

        public void Dispose()
        {
            foreach (var statement in Statements)
                statement.Dispose();
        }
    }
}