using System;
using DS.Analysis.Expressions;
using DS.Analysis.Types;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class IfStatement : Statement
    {
        public IfStatement(ContextInfo context, If syntax)
        {
            // Get the if expression
            Expression @if = context.GetExpression(syntax.Expression);

            AddDisposable(@if.Type.Subscribe(type => {
            }));
        }
    }
}