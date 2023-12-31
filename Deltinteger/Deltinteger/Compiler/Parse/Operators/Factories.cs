using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Compiler.Parse.Operators;

class OstwOperandFactory : IOperandFactory<IParseExpression>
{
    public IParseExpression CreateBinaryExpression(IStackOperator<IParseExpression> op, IParseExpression left, IParseExpression right)
        => new();

    public IParseExpression CreateIndexer(IParseExpression array, IParseExpression index)
        => new();

    public IParseExpression CreateTernary(IParseExpression lhs, IParseExpression middle, IParseExpression rhs)
        => new();

    public IParseExpression CreateUnaryExpression(IStackOperator<IParseExpression> op, IParseExpression value)
        => new();
}