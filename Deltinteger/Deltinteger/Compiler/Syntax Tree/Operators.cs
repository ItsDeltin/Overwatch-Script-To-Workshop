#nullable enable

using Deltin.Deltinteger.Compiler.Parse.Operators;

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

public class BinaryOperatorExpression : Node, IParseExpression
{
    public IParseExpression Left { get; }
    public IParseExpression Right { get; }
    public OperatorNode Operator { get; }

    public BinaryOperatorExpression(IParseExpression left, IParseExpression right, OperatorNode op)
    {
        Left = left;
        Right = right;
        Operator = op;
        Range = left.Range.Start + right.Range.End;
    }

    public override string ToString() => Left.ToString() + " " + Operator.ToString() + " " + Right.ToString();

    public bool IsDotExpression() => Operator.Text == "." || Operator.Text == "~";
}

public class UnaryOperatorExpression : Node, IParseExpression
{
    public IParseExpression Value { get; }
    public OperatorNode Operator { get; }

    public UnaryOperatorExpression(IParseExpression value, OperatorNode op)
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