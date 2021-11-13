using System;
using DS.Analysis.Expressions;
using DS.Analysis.Types;
using DS.Analysis.Structure;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class IfStatement : Statement
    {
        readonly If syntax;

        public IfStatement(ContextInfo context, If syntax)
        {
            this.syntax = syntax;

            // Get the if expression
            Expression @if = context.GetExpression(syntax.Expression);
        }
    }
}