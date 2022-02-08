using DS.Analysis.Expressions;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class ExpressionStatement : Statement
    {
        Expression expression;

        public ExpressionStatement(ContextInfo context, ExpressionStatementSyntax syntax) : base(context)
        {
            AddDisposable(expression = context.GetExpression(syntax.Expression));
        }
    }
}