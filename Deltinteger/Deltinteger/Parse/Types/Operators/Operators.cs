using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public enum TypeOperator
    {
        ///<summary>a ^ b</summary>
        Pow,
        ///<summary>a * b</summary>
        Multiply,
        ///<summary>a / b</summary>
        Divide,
        ///<summary>a % b</summary>
        Modulo,
        ///<summary>a + b</summary>
        Add,
        ///<summary>a - b</summary>
        Subtract,
        ///<summary>a < b</summary>
        LessThan,
        ///<summary>a <= b</summary>
        LessThanOrEqual,
        ///<summary>a == b</summary>
        Equal,
        ///<summary>a >= b</summary>
        GreaterThanOrEqual,
        ///<summary>a > b</summary>
        GreaterThan,
        ///<summary>a != b</summary>
        NotEqual,
        ///<summary>a (and) b</summary>
        And,
        ///<summary>a || b</summary>
        Or,
    }

    public enum UnaryTypeOperator
    {
        Minus,
        Not
    }

    public enum AssignmentOperator
    {
        Equal,
        PowEqual,
        MultiplyEqual,
        DivideEqual,
        ModuloEqual,
        AddEqual,
        SubtractEqual
    }
}