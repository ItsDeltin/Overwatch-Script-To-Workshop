using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Compiler.Parse.Operators;

class OstwOperandFactory : IOperandFactory<IParseExpression>
{
    public IParseExpression CreateBinaryExpression(OperatorNode op, IParseExpression left, IParseExpression right)
        => new BinaryOperatorExpression(left, right, op);

    public IParseExpression CreateIndexer(IParseExpression array, IParseExpression index, DocPos endPos)
        => new ValueInArray(array, index, endPos);

    public IParseExpression CreateTernary(IParseExpression lhs, IParseExpression middle, IParseExpression rhs)
        => new TernaryExpression(lhs, middle, rhs);

    public IParseExpression CreateUnaryExpression(OperatorNode op, IParseExpression value)
        => new UnaryOperatorExpression(value, op);
}