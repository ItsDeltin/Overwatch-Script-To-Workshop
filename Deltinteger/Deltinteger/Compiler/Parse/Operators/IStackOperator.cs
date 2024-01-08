#nullable enable

using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Model;
namespace Deltin.Deltinteger.Compiler.Parse.Operators;

enum OperatorType
{
    Other,
    Sentinel,
    Unary,
    Ternary,
    RhsTernary,
}

interface IStackOperator<T>
{
    int GetPrecedence();
    OperatorType GetOperatorType();
    Result<T, IParserError> ToExpression(IExpressionStackHelper<T> stackHelper);

    public static IStackOperator<T> Sentinel { get; } = new SentinelStackOperator();

    class SentinelStackOperator : IStackOperator<T>
    {
        public int GetPrecedence() => 0;
        public OperatorType GetOperatorType() => OperatorType.Sentinel;
        public Result<T, IParserError> ToExpression(IExpressionStackHelper<T> stackHelper) =>
            throw new NotImplementedException();
    }
}

record IndexerStackOperator<T>(T Index, DocPos EndPosition) : IStackOperator<T>
{
    public OperatorType GetOperatorType() => OperatorType.Other;
    public int GetPrecedence() => CStyleOperator.ArrayIndexPrecedence;
    public Result<T, IParserError> ToExpression(IExpressionStackHelper<T> stackHelper)
    {
        var array = stackHelper.PopOperand();
        return stackHelper.GetFactory().CreateIndexer(array, Index, EndPosition);
    }
}

record InvokeStackOperator(Token LeftParentheses, Token RightParentheses, List<ParameterValue> Values) : IStackOperator<IParseExpression>
{
    public OperatorType GetOperatorType() => OperatorType.Other;
    public int GetPrecedence() => CStyleOperator.InvokePrecedence;
    public Result<IParseExpression, IParserError> ToExpression(IExpressionStackHelper<IParseExpression> stackHelper)
    {
        var value = stackHelper.PopOperand();
        return new FunctionExpression(value, LeftParentheses, RightParentheses, Values);
    }
}

record TypeCastStackOperator(Parser Parser, IParseType CastingTo, DocPos StartPosition) : IStackOperator<IParseExpression>
{
    public OperatorType GetOperatorType() => OperatorType.Other;
    public int GetPrecedence() => CStyleOperator.TypeCastPrecedence;
    public Result<IParseExpression, IParserError> ToExpression(IExpressionStackHelper<IParseExpression> stackHelper)
    {
        var value = stackHelper.PopOperand();
        return Parser.EndNodeFrom(new TypeCast(CastingTo, value), StartPosition);
    }
}