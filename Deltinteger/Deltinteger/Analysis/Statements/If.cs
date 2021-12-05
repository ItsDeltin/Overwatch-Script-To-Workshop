using DS.Analysis.Expressions;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class IfStatement : Statement
    {
        readonly Expression @if;
        readonly Statement block;

        public IfStatement(ContextInfo context, If syntax)
        {
            // Get the if expression
            AddDisposable(@if = context.GetExpression(syntax.Expression));

            // Get the block
            AddDisposable(block = context.StatementFromSyntax(syntax.Statement));
        }
    }
}