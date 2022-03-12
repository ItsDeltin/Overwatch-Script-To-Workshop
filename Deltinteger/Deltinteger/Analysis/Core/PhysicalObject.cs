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


        protected PhysicalObject(ContextInfo context) : base(context.Analysis)
        {
            Context = context;
        }


        protected IExpressionHost GetExpression(IParseExpression syntax, ContextInfo context = null)
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
            DependOn(Context.Scope);
        }
    }
}