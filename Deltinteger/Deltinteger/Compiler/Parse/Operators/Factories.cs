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

class VanillaOperandFactory : IOperandFactory<IVanillaExpression>
{
    public IVanillaExpression CreateBinaryExpression(OperatorNode op, IVanillaExpression left, IVanillaExpression right)
        => new VanillaBinaryOperatorExpression(left, op.Token, right);

    public IVanillaExpression CreateIndexer(IVanillaExpression array, IVanillaExpression index, DocPos endPos)
    {
        throw new System.NotImplementedException();
    }

    public IVanillaExpression CreateTernary(IVanillaExpression lhs, IVanillaExpression middle, IVanillaExpression rhs)
    {
        throw new System.NotImplementedException();
    }

    public IVanillaExpression CreateUnaryExpression(OperatorNode op, IVanillaExpression value)
    {
        throw new System.NotImplementedException();
    }
}