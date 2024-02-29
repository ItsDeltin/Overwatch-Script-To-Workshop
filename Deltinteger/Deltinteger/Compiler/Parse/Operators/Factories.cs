#nullable enable

using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Compiler.Parse.Operators;

class OstwOperandFactory : IOperandFactory<IParseExpression>
{
    public IParseExpression CreateBinaryExpression(OperatorNode op, IParseExpression left, IParseExpression right)
        => new BinaryOperatorExpression(left, right, op);

    public IParseExpression CreateIndexer(IParseExpression array, Token leftBracket, IParseExpression index, Token? rightBracket)
        => new ValueInArray(array, index, (rightBracket?.Range ?? index.Range).End);

    public IParseExpression CreateTernary(IParseExpression lhs, IParseExpression middle, IParseExpression rhs)
        => new TernaryExpression(lhs, middle, rhs);

    public IParseExpression CreateUnaryExpression(OperatorNode op, IParseExpression value)
        => new UnaryOperatorExpression(value, op);
}

class VanillaOperandFactory : IOperandFactory<IVanillaExpression>
{
    public IVanillaExpression CreateBinaryExpression(OperatorNode op, IVanillaExpression left, IVanillaExpression right)
        => new VanillaBinaryOperatorExpression(left, op.Token, right);

    public IVanillaExpression CreateIndexer(IVanillaExpression array, Token leftBracket, IVanillaExpression index, Token? rightBracket)
        => new VanillaIndexerExpression(array, leftBracket, index, rightBracket);

    public IVanillaExpression CreateTernary(IVanillaExpression lhs, IVanillaExpression middle, IVanillaExpression rhs)
        => new VanillaTernaryExpression(lhs, middle, rhs);

    public IVanillaExpression CreateUnaryExpression(OperatorNode op, IVanillaExpression value)
        => new VanillaNotExpression(op.Token, value);
}