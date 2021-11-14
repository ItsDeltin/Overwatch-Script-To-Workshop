using DS.Analysis.Expressions;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class ExpressionStatement : Statement
    {
        Expression expression;

        public ExpressionStatement(ContextInfo contextInfo, ExpressionStatementSyntax expressionStatement)
        {
            AddDisposable(expression = contextInfo.GetExpression(expressionStatement.Expression));
        }
    }
}