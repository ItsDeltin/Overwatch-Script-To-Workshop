#nullable enable

using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse;

sealed class IsExpression : IExpression
{
    readonly CodeType returnType;
    readonly MatchedEnumPattern? enumPattern;

    public IsExpression(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression op)
    {
        var lhs = parseInfo.GetExpression(scope, op.Left);
        returnType = parseInfo.Types.Boolean();
        var operand = PatternMatching.GetPatternOperand(parseInfo, lhs);
        enumPattern = PatternMatching.GetPattern(parseInfo, scope, op.Operator.Token, op.Right, operand);
    }

    public IWorkshopTree Parse(ActionSet actionSet)
    {
        return enumPattern.ToWorkshop(actionSet);
    }

    public CodeType Type() => returnType;
}