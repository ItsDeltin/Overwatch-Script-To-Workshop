using Deltin.Deltinteger.Decompiler.ElementToCode;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class TTEOperator
    {
        public static TTEOperator Sentinel { get; } = new TTEOperator(0, null);
        // Unary
        public static TTEOperator Not { get; } = new TTEOperator(16, "!", OperatorType.Unary);
        // Compare
        public static TTEOperator Ternary { get; } = new TTEOperator(1, "?", OperatorType.Ternary);
        public static TTEOperator RhsTernary { get; } = new TTEOperator(2, ":", OperatorType.Ternary);
        public static TTEOperator Equal { get; } = new TTEOperator(3, "==");
        public static TTEOperator NotEqual { get; } = new TTEOperator(4, "!=");
        public static TTEOperator GreaterThan { get; } = new TTEOperator(5, ">");
        public static TTEOperator LessThan { get; } = new TTEOperator(6, "<");
        public static TTEOperator GreaterThanOrEqual { get; } = new TTEOperator(7, ">=");
        public static TTEOperator LessThanOrEqual { get; } = new TTEOperator(8, "<=");
        // Boolean
        public static TTEOperator And { get; } = new TTEOperator(9, "&&");
        public static TTEOperator Or { get; } = new TTEOperator(10, "||");
        // Math
        public static TTEOperator Subtract { get; } = new TTEOperator(11, "-");
        public static TTEOperator Add { get; } = new TTEOperator(12, "+");
        public static TTEOperator Modulo { get; } = new TTEOperator(13, "%");
        public static TTEOperator Divide { get; } = new TTEOperator(14, "/");
        public static TTEOperator Multiply { get; } = new TTEOperator(15, "*");
        public static TTEOperator Power { get; } = new TTEOperator(16, "^");

        public int Precedence { get; }
        public string Operator { get; }
        public OperatorType Type { get; }

        public TTEOperator(int precedence, string op, OperatorType type = OperatorType.Binary)
        {
            Precedence = precedence;
            Operator = op;
            Type = type;
        }

        public static bool Compare(TTEOperator op1, TTEOperator op2)
        {
            if ((op1 == Ternary || op1 == RhsTernary) && (op2 == Ternary || op2 == RhsTernary))
                return op1 == RhsTernary && op2 == RhsTernary;
            
            if (op1 == Sentinel || op2 == Sentinel) return false;
            return op1.Precedence > op2.Precedence;
        }
    }

    public enum OperatorType
    {
        Unary,
        Binary,
        Ternary
    }

    public class BinaryOperatorExpression : ITTEExpression
    {
        public ITTEExpression Left { get; }
        public ITTEExpression Right { get; }
        public TTEOperator Operator { get; }

        public BinaryOperatorExpression(ITTEExpression left, ITTEExpression right, TTEOperator op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        public override string ToString() => Left.ToString() + " " + Operator.Operator + " " + Right.ToString();

        public void Decompile(DecompileRule decompiler)
        {
            WriteSide(decompiler, Left);
            decompiler.Append(" " + Operator.Operator + " ");
            WriteSide(decompiler, Right);
        }

        private void WriteSide(DecompileRule decompiler, ITTEExpression expression)
        {
            if (expression is TernaryExpression || (expression is BinaryOperatorExpression bop && bop.Operator.Precedence < Operator.Precedence))
            {
                decompiler.Append("(");
                expression.Decompile(decompiler);
                decompiler.Append(")");
            }
            else
                expression.Decompile(decompiler);
        }
    }

    public class UnaryOperatorExpression : ITTEExpression
    {
        public ITTEExpression Value { get; }
        public TTEOperator Operator { get; }

        public UnaryOperatorExpression(ITTEExpression value, TTEOperator op)
        {
            Value = value;
            Operator = op;
        }

        public override string ToString() => Operator.Operator + Value.ToString();

        public void Decompile(DecompileRule decompiler)
        {
            decompiler.Append(Operator.Operator);

            if (Value is BinaryOperatorExpression || Value is TernaryExpression)
            {
                decompiler.Append("(");
                Value.Decompile(decompiler);
                decompiler.Append(")");
            }
            else
                Value.Decompile(decompiler);
        }
    }

    public class TernaryExpression : ITTEExpression
    {
        public ITTEExpression Condition { get; }
        public ITTEExpression Consequent { get; }
        public ITTEExpression Alternative { get; }

        public TernaryExpression(ITTEExpression condition, ITTEExpression consequent, ITTEExpression alternative)
        {
            Condition = condition;
            Consequent = consequent;
            Alternative = alternative;
        }

        public override string ToString() => "(" + Condition.ToString() + " ? " + Consequent.ToString() + " : " + Alternative.ToString() + ")";

        public void Decompile(DecompileRule decompiler)
        {
            Condition.Decompile(decompiler);
            decompiler.Append(" ? ");
            Consequent.Decompile(decompiler);
            decompiler.Append(" : ");
            Alternative.Decompile(decompiler);
        }
    }
}