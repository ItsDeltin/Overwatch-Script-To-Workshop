#nullable enable

using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse;

sealed class IsExpression : IExpression
{
    readonly IExpression lhs;
    readonly CodeType returnType;
    readonly MatchedEnumPattern? enumPattern;

    public IsExpression(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression op)
    {
        lhs = parseInfo.GetExpression(scope, op.Left);
        returnType = parseInfo.Types.Boolean();
        enumPattern = PatternMatching.GetPattern(parseInfo, scope, op.Right);

        new VariableResolve(parseInfo, new(), lhs, op.Left.Range);
    }

    public IWorkshopTree Parse(ActionSet actionSet)
    {
        var operand = lhs.Parse(actionSet);
        return enumPattern.ToWorkshop(actionSet, operand);
    }

    public CodeType Type() => returnType;
}