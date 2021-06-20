using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public interface IParserError
    {
        DocRange Range { get; }
        string Message();
        Diagnostic GetDiagnostic() => new Diagnostic(Message(), Range, Diagnostic.Error);
    }

    class ExpectedTokenError : IParserError
    {
        public DocRange Range { get; }
        public TokenType[] Expecting { get; }

        public ExpectedTokenError(DocRange range, params TokenType[] expecting)
        {
            Range = range;
            Expecting = expecting;
        }

        public string Message() => string.Join(", ", Expecting.Select(e => e.Name())) + " expected";
        public override string ToString() => "[" + Message() + ", range: " + Range.ToString() + "]";
    }

    class InvalidExpressionTerm : IParserError
    {
        public DocRange Range { get; }
        public TokenType ObtainedToken { get; }

        public InvalidExpressionTerm(Token invalid)
        {
            Range = invalid.Range;
            ObtainedToken = invalid.TokenType;
        }

        public string Message() => "Invalid expression term '" + ObtainedToken.Name() + "'";
        public override string ToString() => "[" + Message() + ", range: " + Range.ToString() + "]";
    }

    class UnexpectedToken : IParserError
    {
        public DocRange Range { get; }
        public TokenType Type { get; }

        public UnexpectedToken(Token token)
        {
            Range = token.Range;
            Type = token.TokenType;
        }

        public string Message() => "Unexpected token '" + Type.Name() + "'";
    }

    class MissingTernaryHand : IParserError
    {
        public DocRange Range { get; }
        private readonly bool _left;

        public MissingTernaryHand(Token token, bool left)
        {
            Range = token.Range;
            _left = left;
        }

        public string Message() => "No " + (_left ? "left" : "right") + "-hand ternary operator";
    }

    class InterpolationMissingTerminator : IParserError
    {
        public DocRange Range { get; }

        public InterpolationMissingTerminator(DocRange range)
        {
            Range = range;
        }

        public string Message() => "Missing close delimiter '}' for interpolated expression started with '{'.";
    }
}