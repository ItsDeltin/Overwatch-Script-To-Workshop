using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Core
{
    using Scopes;
    using Expressions;
    using Statements;

    abstract class PhysicalObject : AnalysisObject
    {
        /// <summary>The current analysis context.</summary>
        protected ContextInfo Context { get; }

        /// <summary>Gets the elements in the current scope.
        /// Will throw an exception if <see cref="DependOnScope"/> is not called.</summary>
        protected ScopedElement[] ScopedElements
        {
            get
            {
                if (watcher == null)
                    throw new Exception("AnalysisObject.DependOnScope() must be called before AnalysisObject.ScopedElements can be used.");

                return watcher.Elements;
            }
        }

        private ScopeWatcher watcher;


        protected PhysicalObject(ContextInfo context) : base(context.Analysis)
        {
            Context = context;
        }


        protected Expression GetExpression(IParseExpression syntax, ContextInfo context = null)
        {
            var expr = (context ?? Context).GetExpression(syntax);
            DependOnAndHost(expr);
            return expr;
        }

        protected Statement GetStatement(IParseStatement syntax, ContextInfo context = null)
        {
            var statement = (context ?? Context).StatementFromSyntax(syntax);
            DependOnAndHost(statement);
            return statement;
        }

        protected void DependOnScope()
        {
            watcher = Context.Scope.Watch(Context.Analysis);
            DependOn(watcher);
        }

        protected ScopeWatcher DependOnExternalScope(Scope scope)
        {
            var watcher = scope.Watch(Context.Analysis);
            DependOn(watcher);
            return watcher;
        }
    }
}