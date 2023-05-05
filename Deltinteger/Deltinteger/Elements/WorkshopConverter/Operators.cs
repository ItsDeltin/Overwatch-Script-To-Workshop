namespace Deltin.Deltinteger.Elements.WorkshopConverter;
using System;

enum Op
{
    Power,
    Multiply,
    Divide,
    Modulo,
    Add,
    Subtract,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal,
    NotEqual,
    Or,
    And
}

static class WorkshopOperators
{
    public const int ARRAY_SUBSCRIPT = 0;
    public const int UNARY = 1;
    public const int POWER = 2;
    public const int MULTIPLICATION_DIVISION_REMAINDER = 3;
    public const int ADDITION_SUBTRACTION = 4;
    public const int COMPARISON = 5;
    public const int RELATIONAL = 6;
    public const int LOGICAL = 7;
    public const int TERNARY = 8;

    public static string WorkshopSymbolFromOp(Op op)
    {
        switch (op)
        {
            case Op.Power: return "^";
            case Op.Multiply: return "*";
            case Op.Divide: return "/";
            case Op.Modulo: return "%";
            case Op.Add: return "+";
            case Op.Subtract: return "-";
            case Op.GreaterThan: return ">";
            case Op.LessThan: return "<";
            case Op.GreaterThanOrEqual: return ">=";
            case Op.LessThanOrEqual: return "<=";
            case Op.Equal: return "==";
            case Op.NotEqual: return "!=";
            case Op.Or: return "||";
            case Op.And: return "&&";
            default: throw new NotImplementedException(op.ToString());
        }
    }

    public static int GetPrecedence(Op op)
    {
        switch (op)
        {
            case (Op.Power): return POWER;
            case (Op.Multiply): return MULTIPLICATION_DIVISION_REMAINDER;
            case (Op.Divide): return MULTIPLICATION_DIVISION_REMAINDER;
            case (Op.Modulo): return MULTIPLICATION_DIVISION_REMAINDER;
            case (Op.Add): return ADDITION_SUBTRACTION;
            case (Op.Subtract): return ADDITION_SUBTRACTION;
            case (Op.GreaterThan): return COMPARISON;
            case (Op.LessThan): return COMPARISON;
            case (Op.GreaterThanOrEqual): return COMPARISON;
            case (Op.LessThanOrEqual): return COMPARISON;
            case (Op.Equal): return RELATIONAL;
            case (Op.NotEqual): return RELATIONAL;
            case (Op.Or): return LOGICAL;
            case (Op.And): return LOGICAL;
            default: throw new NotImplementedException(op.ToString());
        }
    }
}