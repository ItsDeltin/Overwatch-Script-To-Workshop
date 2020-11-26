using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Strings
{
    public class InterpolatedStringAction : IExpression
    {
        private readonly ParseInfo _parseInfo;
        private readonly string _tail;
        private readonly InterpolatedStringActionPart[] _parts;

        public InterpolatedStringAction(InterpolatedStringExpression context, ParseInfo parseInfo, Scope scope)
        {
            _parseInfo = parseInfo;
            _tail = GetPartText(context.Tail);

            _parts = new InterpolatedStringActionPart[context.Parts == null ? 0 : context.Parts.Count];
            for (int i = 0; i < _parts.Length; i++)
                _parts[i] = new InterpolatedStringActionPart(parseInfo.GetExpression(scope, context.Parts[i].Expression), GetPartText(context.Parts[i].Right));
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            var values = new IWorkshopTree[1 + _parts.Length * 2];
            values[0] = Element.CustomString(_tail);
            
            for (int i = 0; i < _parts.Length; i++)
            {
                values[1 + i * 2] = _parts[i].Value.Parse(actionSet);
                values[2 + i * 2] = Element.CustomString(_parts[i].Right);
            }

            return StringElement.Join(values);
        }

        public CodeType Type() => _parseInfo.TranslateInfo.Types.String();

        static string GetPartText(Token token)
        {
            switch (token.TokenType)
            {
                case Compiler.TokenType.InterpolatedStringTail:
                    string reduce = token.Text.Substring(1).TrimStart();
                    return reduce.Substring(1, reduce.Length - 2);
                case Compiler.TokenType.InterpolatedStringMiddle:
                case Compiler.TokenType.InterpolatedStringHead:
                    return token.Text.Substring(0, token.Text.Length - 1);
                default:
                    return token.Text.RemoveQuotes();
            }
        }

        struct InterpolatedStringActionPart
        {
            public IExpression Value;
            public string Right;

            public InterpolatedStringActionPart(IExpression value, string right)
            {
                Value = value;
                Right = right;
            }
        }
    }
}