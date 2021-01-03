using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.Parse;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public interface ICompilerOperator
    {
        int Precedence { get; }
    }

    public class CompilerOperator : ICompilerOperator
    {
        public static CompilerOperator Sentinel { get; } = new CompilerOperator();
        public static CompilerOperator Squiggle { get; } = new CompilerOperator(1, "~", TokenType.Squiggle) { RhsHandler = new DotRhsHandler() };
        // Compare
        public static CompilerOperator Ternary { get; } = new CompilerOperator(2, "?", TokenType.QuestionMark, OperatorType.TernaryLeft);
        public static CompilerOperator RhsTernary { get; } = new CompilerOperator(3, ":", TokenType.Colon, OperatorType.TernaryRight);

        public static CompilerOperator Or { get; } = new CompilerOperator(4, "||", TokenType.Or);
        public static CompilerOperator And { get; } = new CompilerOperator(5, "&&", TokenType.And);

        public static CompilerOperator Equal { get; } = new CompilerOperator(6, "==", TokenType.EqualEqual);
        public static CompilerOperator NotEqual { get; } = new CompilerOperator(6, "!=", TokenType.NotEqual);
        public static CompilerOperator GreaterThan { get; } = new CompilerOperator(7, ">", TokenType.GreaterThan);
        public static CompilerOperator LessThan { get; } = new CompilerOperator(7, "<", TokenType.LessThan);
        public static CompilerOperator GreaterThanOrEqual { get; } = new CompilerOperator(7, ">=", TokenType.GreaterThanOrEqual);
        public static CompilerOperator LessThanOrEqual { get; } = new CompilerOperator(7, "<=", TokenType.LessThanOrEqual);
        // Boolean
        // Math
        public static CompilerOperator Subtract { get; } = new CompilerOperator(8, "-", TokenType.Subtract);
        public static CompilerOperator Add { get; } = new CompilerOperator(8, "+", TokenType.Add);
        public static CompilerOperator Modulo { get; } = new CompilerOperator(9, "%", TokenType.Modulo);
        public static CompilerOperator Divide { get; } = new CompilerOperator(9, "/", TokenType.Divide);
        public static CompilerOperator Multiply { get; } = new CompilerOperator(9, "*", TokenType.Multiply);
        public static CompilerOperator Power { get; } = new CompilerOperator(10, "^", TokenType.Hat);
        // Unary
        public static CompilerOperator Not { get; } = new CompilerOperator(11, "!", TokenType.Exclamation, OperatorType.Unary);
        public static CompilerOperator Inv { get; } = new CompilerOperator(11, "-", TokenType.Subtract, OperatorType.Unary);
        // Dot
        public static CompilerOperator Dot { get; } = new CompilerOperator(13, ".", TokenType.Dot) { RhsHandler = new DotRhsHandler() };

        // Lists
        public static CompilerOperator[] BinaryOperators { get; } = new CompilerOperator[] {
            Squiggle, Dot, Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, And, Or, Subtract, Add, Modulo, Divide, Multiply, Power,
            Ternary, RhsTernary
        };
        public static CompilerOperator[] UnaryOperators { get; } = new CompilerOperator[] { Not, Inv };

        public int Precedence { get; }
        public string Operator { get; }
        public TokenType RelatedToken { get; }
        public OperatorType Type { get; }
        public IOperatorRhsHandler RhsHandler { get; set; } = new DefaultRhsHandler();

        public CompilerOperator()
        {
            Precedence = 0;
            RelatedToken = TokenType.Unknown;
            Operator = null;
            Type = OperatorType.Binary;
        }

        public CompilerOperator(int precedence, string op, TokenType relatedToken, OperatorType type = OperatorType.Binary)
        {
            Precedence = precedence;
            RelatedToken = relatedToken;
            Operator = op;
            Type = type;
        }

        public override string ToString() => Operator;

        public static bool Compare(ICompilerOperator op1, ICompilerOperator op2)
        {
            if ((op1 == Ternary || op1 == RhsTernary) && (op2 == Ternary || op2 == RhsTernary))
                return op1 == RhsTernary && op2 == RhsTernary;

            if (op1 == Sentinel || op2 == Sentinel) return false;

            return op1.Precedence >= op2.Precedence;
        }
    }

    public enum OperatorType
    {
        Unary,
        Binary,
        TernaryLeft,
        TernaryRight,
    }

    public interface IOperatorRhsHandler
    {
        void Get(Parser parser);
    }

    public class DefaultRhsHandler : IOperatorRhsHandler
    {
        public void Get(Parser parser)
        {
            parser.GetExpressionWithArray();
        }
    }

    public class DotRhsHandler : IOperatorRhsHandler
    {
        public void Get(Parser parser)
        {
            parser.Operands.Push(parser.Identifier());
            parser.GetArrayAndInvokes();
        }
    }

    public class TypeCastOperator : ICompilerOperator
    {
        public static TypeCastOperator Instance { get; } = new TypeCastOperator();
        public int Precedence => 11;
    }

    public class ArrayOperator : ICompilerOperator
    {
        public static ArrayOperator Instance { get; } = new ArrayOperator();
        public int Precedence => 12;
    }

    public class InvokeOperator : ICompilerOperator
    {
        public static InvokeOperator Instance { get; } = new InvokeOperator();
        public int Precedence => 14;
    }
}

namespace Deltin.Deltinteger.Compiler.SyntaxTree
{
    public interface IOperatorInfo
    {
        ICompilerOperator Source { get; }
    }

    public class OperatorInfo : IOperatorInfo
    {
        public static OperatorInfo Sentinel { get; } = new OperatorInfo(CompilerOperator.Sentinel, null);

        public CompilerOperator Operator { get; }
        public Token Token { get; }
        public OperatorType Type => Operator.Type;
        public int Precedence => Operator.Precedence;
        ICompilerOperator IOperatorInfo.Source => Operator;

        public OperatorInfo(CompilerOperator compilerOperator, Token token)
        {
            Operator = compilerOperator;
            Token = token;
        }

        public override string ToString() => Operator.ToString();
    }

    public class TypeCastInfo : IOperatorInfo
    {
        public ICompilerOperator Source => TypeCastOperator.Instance;
        public IParseType CastingTo { get; }
        public DocPos StartPosition { get; }

        public TypeCastInfo(IParseType castingTo, DocPos startPosition)
        {
            CastingTo = castingTo;
            StartPosition = startPosition;
        }
    }

    public class ValueInArrayInfo : IOperatorInfo
    {
        public ICompilerOperator Source => ArrayOperator.Instance;
        public IParseExpression Index { get; }
        public DocPos EndPosition { get; }

        public ValueInArrayInfo(IParseExpression index, DocPos endPosition)
        {
            Index = index;
            EndPosition = endPosition;
        }
    }

    public class InvokeInfo : IOperatorInfo
    {
        public ICompilerOperator Source => InvokeOperator.Instance;
        public Token LeftParentheses { get; }
        public Token RightParentheses { get; }
        public List<ParameterValue> Values { get; }

        public InvokeInfo(Token leftParentheses, Token rightParentheses, List<ParameterValue> values)
        {
            LeftParentheses = leftParentheses;
            RightParentheses = rightParentheses;
            Values = values;
        }
    }

    public class BinaryOperatorExpression : Node, IParseExpression
    {
        public IParseExpression Left { get; }
        public IParseExpression Right { get; }
        public OperatorInfo Operator { get; }

        public BinaryOperatorExpression(IParseExpression left, IParseExpression right, OperatorInfo op)
        {
            Left = left;
            Right = right;
            Operator = op;
            Range = left.Range.Start + right.Range.End;
        }

        public override string ToString() => Left.ToString() + " " + Operator.ToString() + " " + Right.ToString();

        public bool IsDotExpression() => Operator.Operator == CompilerOperator.Dot || Operator.Operator == CompilerOperator.Squiggle;
    }

    public class UnaryOperatorExpression : Node, IParseExpression
    {
        public IParseExpression Value { get; }
        public OperatorInfo Operator { get; }

        public UnaryOperatorExpression(IParseExpression value, OperatorInfo op)
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
}