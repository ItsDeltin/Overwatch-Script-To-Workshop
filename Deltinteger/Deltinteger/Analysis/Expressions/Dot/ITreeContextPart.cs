using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Dot
{
    interface ITreeContextPart
    {
        Expression GetExpression(ContextInfo contextInfo);
    }

    class ExpressionPart : ITreeContextPart
    {
        readonly IParseExpression expressionSyntax;
        public ExpressionPart(IParseExpression expressionSyntax) => this.expressionSyntax = expressionSyntax;
        public Expression GetExpression(ContextInfo contextInfo) => contextInfo.GetExpression(expressionSyntax);
    }
}