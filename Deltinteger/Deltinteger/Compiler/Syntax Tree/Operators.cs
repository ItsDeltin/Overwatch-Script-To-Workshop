#nullable enable

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

public class BinaryOperatorExpression : Node, IParseExpression
{
    public IParseExpression Left { get; }
    public IParseExpression Right { get; }
    public OperatorInfo Operator { get; }

    public BinaryOperatorExpression(IParseExpression left, IParseExpression right, OperatorInfo op)
    {
        Left = left;
        Right = right;
        Operator = op;
        Range = left.Range.Start + right.Range.End;
    }

    public override string ToString() => Left.ToString() + " " + Operator.ToString() + " " + Right.ToString();

    public bool IsDotExpression() => Operator.Operator == CompilerOperator.Dot || Operator.Operator == CompilerOperator.Squiggle;
}

public class UnaryOperatorExpression : Node, IParseExpression
{
    public IParseExpression Value { get; }
    public OperatorInfo Operator { get; }

    public UnaryOperatorExpression(IParseExpression value, OperatorInfo op)
    {
        Value = value;
        Operator = op;
        Range = op.Token.Range.Start + value.Range.End;
    }

    public override string ToString() => Operator.ToString() + Value.ToString();
}

public class TernaryExpression : Node, IParseExpression
{
    public IParseExpression Condition { get; }
    public IParseExpression Consequent { get; }
    public IParseExpression Alternative { get; }

    public TernaryExpression(IParseExpression condition, IParseExpression consequent, IParseExpression alternative)
    {
        Condition = condition;
        Consequent = consequent;
        Alternative = alternative;
        Range = condition.Range.Start + alternative.Range.End;
    }

    public override string ToString() => Condition.ToString() + " ? " + Consequent.ToString() + " : " + Alternative.ToString();
}