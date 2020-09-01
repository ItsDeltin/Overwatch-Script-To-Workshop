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
}