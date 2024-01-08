#nullable enable

using System;
using System.Collections.Generic;
namespace Deltin.Deltinteger.Compiler.Parse.Operators;

class OperatorStack<T> : IExpressionStackHelper<T>
{
    readonly Stack<T> operands = new();
    readonly Stack<IStackOperator<T>> operators = new();
    readonly IOperandFactory<T> operandFactory;

    public OperatorStack(IOperandFactory<T> operandFactory)
    {
        this.operandFactory = operandFactory;
    }

    public T GetExpression(Action getExpression)
    {
        operators.Push(IStackOperator<T>.Sentinel);
        getExpression();
        PopAllOperators(); // Pop until sentinel
        operators.Pop(); // Pop sentinel
        return operands.Pop();
    }

    public void PushOperator(IStackOperator<T> op)
    {
        while (ShouldPop(operators.Peek(), op))
            PopOperator();
        operators.Push(op);
    }

    public void PushOperand(T operand) => operands.Push(operand);

    public void PopAllOperators()
    {
        while (operators.Peek().GetPrecedence() > 0)
            PopOperator();
    }

    void PopOperator()
    {
        var iop = operators.Pop();
        iop.ToExpression(this).Match(operands.Push, err =>
        {
            // todo: should we add missing value to operands?
            // todo: handle error here
        });
    }

    static bool ShouldPop(IStackOperator<T> last, IStackOperator<T> pushing)
    {
        var op1 = last.GetOperatorType();
        var op2 = pushing.GetOperatorType();

        if ((op1 == OperatorType.Ternary || op1 == OperatorType.RhsTernary) &&
            (op2 == OperatorType.Ternary || op2 == OperatorType.RhsTernary))
            return op1 == OperatorType.RhsTernary && op2 == OperatorType.RhsTernary;

        if (op1 == OperatorType.Sentinel || op2 == OperatorType.Sentinel) return false;

        if (op1 == OperatorType.Unary && op2 == OperatorType.Unary) return false;

        return last.GetPrecedence() >= pushing.GetPrecedence();
    }

    // IExpressionStackHelper
    T IExpressionStackHelper<T>.PopOperand() => operands.Pop();
    IStackOperator<T> IExpressionStackHelper<T>.NextOperator() => operators.Peek();
    IStackOperator<T> IExpressionStackHelper<T>.PopOperator() => operators.Pop();
    IOperandFactory<T> IExpressionStackHelper<T>.GetFactory() => operandFactory;
}

interface IOperandFactory<T>
{
    T CreateBinaryExpression(OperatorNode op, T left, T right);
    T CreateUnaryExpression(OperatorNode op, T value);
    T CreateTernary(T lhs, T middle, T rhs);
    T CreateIndexer(T array, T index, DocPos endPos);
}

interface IExpressionStackHelper<T>
{
    T PopOperand();
    IStackOperator<T> NextOperator();
    IStackOperator<T> PopOperator();
    IOperandFactory<T> GetFactory();
}

public record struct OperatorNode(Token Token, string Text);