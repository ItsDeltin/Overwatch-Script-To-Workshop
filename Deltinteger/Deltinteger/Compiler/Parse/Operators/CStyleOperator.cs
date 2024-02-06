#nullable enable

using Deltin.Deltinteger.Model;
namespace Deltin.Deltinteger.Compiler.Parse.Operators;

public enum CStyleOperatorType
{
    Unary,
    Binary,
    TernaryLeft,
    TernaryRight,
}

record CStyleOperator(int Precedence, string Text, CStyleOperatorType Type, TokenType OstwTokenType)
{
    public IStackOperator<T> AsStackOperator<T>(Token token) => new CStyleStackOperator<T>(this, token);

    record class CStyleStackOperator<T>(CStyleOperator CStyleOperator, Token Token) : IStackOperator<T>
    {
        public int GetPrecedence() => CStyleOperator.Precedence;
        public Result<T, IParserError> ToExpression(IExpressionStackHelper<T> stackHelper)
        {
            return CStyleOperator.Type switch
            {
                CStyleOperatorType.Binary => Binary(stackHelper),
                CStyleOperatorType.Unary => Unary(stackHelper),
                CStyleOperatorType.TernaryLeft => LhsTernary(stackHelper),
                CStyleOperatorType.TernaryRight => Ternary(stackHelper),
                _ => throw new System.NotImplementedException(),
            };
        }

        // Binary
        Result<T, IParserError> Binary(IExpressionStackHelper<T> stackHelper)
        {
            var right = stackHelper.PopOperand();
            var left = stackHelper.PopOperand();
            return stackHelper.GetFactory().CreateBinaryExpression(new(Token, CStyleOperator.Text), left, right);
        }

        // Unary
        Result<T, IParserError> Unary(IExpressionStackHelper<T> stackHelper)
        {
            var value = stackHelper.PopOperand();
            return stackHelper.GetFactory().CreateUnaryExpression(new(Token, CStyleOperator.Text), value);
        }

        // Extraneous left-hand ternary
        Result<T, IParserError> LhsTernary(IExpressionStackHelper<T> stackHelper)
        {
            // discard
            stackHelper.PopOperand();
            return new MissingTernaryHand(Token, false);
        }

        // Ternary
        Result<T, IParserError> Ternary(IExpressionStackHelper<T> stackHelper)
        {
            if (stackHelper.NextOperator() is CStyleStackOperator<T> peek &&
                peek.CStyleOperator.Type == CStyleOperatorType.TernaryLeft)
            {
                var op2 = stackHelper.PopOperator();
                var rhs = stackHelper.PopOperand();
                var middle = stackHelper.PopOperand();
                var lhs = stackHelper.PopOperand();
                return stackHelper.GetFactory().CreateTernary(lhs, middle, rhs);
            }
            // Missing left-hand ?
            else
            {
                // discard
                stackHelper.PopOperand();
                return new MissingTernaryHand(Token, true);
            }
        }

        public OperatorType GetOperatorType() => CStyleOperator.Type switch
        {
            CStyleOperatorType.Unary => OperatorType.Unary,
            CStyleOperatorType.TernaryLeft => OperatorType.Ternary,
            CStyleOperatorType.TernaryRight => OperatorType.RhsTernary,
            _ => OperatorType.Other,
        };
    }

    public static CStyleOperator Squiggle { get; } = DotOp(1, "~", TokenType.Squiggle);
    // Compare
    public static CStyleOperator Ternary { get; } = new(2, "?", CStyleOperatorType.TernaryLeft, TokenType.QuestionMark);
    public static CStyleOperator RhsTernary { get; } = new(3, ":", CStyleOperatorType.TernaryRight, TokenType.Colon);

    public static CStyleOperator Or { get; } = Binary(4, "||", TokenType.Or);
    public static CStyleOperator And { get; } = Binary(5, "&&", TokenType.And);

    public static CStyleOperator Equal { get; } = Binary(6, "==", TokenType.EqualEqual);
    public static CStyleOperator NotEqual { get; } = Binary(6, "!=", TokenType.NotEqual);
    public static CStyleOperator GreaterThan { get; } = Binary(7, ">", TokenType.GreaterThan);
    public static CStyleOperator LessThan { get; } = Binary(7, "<", TokenType.LessThan);
    public static CStyleOperator GreaterThanOrEqual { get; } = Binary(7, ">=", TokenType.GreaterThanOrEqual);
    public static CStyleOperator LessThanOrEqual { get; } = Binary(7, "<=", TokenType.LessThanOrEqual);
    // Boolean
    // Math
    public static CStyleOperator Subtract { get; } = Binary(8, "-", TokenType.Subtract);
    public static CStyleOperator Add { get; } = Binary(8, "+", TokenType.Add);
    public static CStyleOperator Modulo { get; } = Binary(9, "%", TokenType.Modulo);
    public static CStyleOperator Divide { get; } = Binary(9, "/", TokenType.Divide);
    public static CStyleOperator Multiply { get; } = Binary(9, "*", TokenType.Multiply);
    public static CStyleOperator Power { get; } = Binary(10, "^", TokenType.Hat);
    // Unary
    public static CStyleOperator Not { get; } = Unary(11, "!", TokenType.Exclamation);
    public static CStyleOperator Inv { get; } = Unary(11, "-", TokenType.Subtract);
    // Dot
    public static CStyleOperator Dot { get; } = DotOp(13, ".", TokenType.Dot);
    // Some more precedence data for other types of operators.
    public const int TypeCastPrecedence = 11;
    public const int ArrayIndexPrecedence = 13;
    public const int InvokePrecedence = 14;

    static CStyleOperator Binary(int precedence, string text, TokenType tokenType) => new(precedence, text, CStyleOperatorType.Binary, tokenType);
    static CStyleOperator Unary(int precedence, string text, TokenType tokenType) => new(precedence, text, CStyleOperatorType.Unary, tokenType);
    static CStyleOperator DotOp(int precedence, string text, TokenType tokenType) => Binary(precedence, text, tokenType);

    public static CStyleOperator[] BinaryOperators { get; } = new[] {
        Squiggle, Dot, Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, And, Or, Subtract, Add, Modulo, Divide, Multiply, Power,
        Ternary, RhsTernary
    };
    public static CStyleOperator[] UnaryOperators { get; } = new[] { Not, Inv };

}