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

        public IfStatement(StructureContext context, If syntax)
        {
            this.syntax = syntax;
        }

        public override void GetContent(ContextInfo contextInfo)
        {
            // Get the if expression
            Expression @if = contextInfo.GetExpression(syntax.Expression);
        }
    }
}