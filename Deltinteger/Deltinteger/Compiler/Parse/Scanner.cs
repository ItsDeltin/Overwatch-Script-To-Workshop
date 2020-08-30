using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class Scanner
    {
        private static readonly char[] identifierCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();
        private static readonly char[] numericalCharacters = "0123456789".ToCharArray();
        private static readonly char[] whitespaceCharacters = " \r\n\t".ToCharArray();

        string Content;
        int Position;
        int Line;
        int Column;
        char Current => Content[Position];
        readonly List<int> _lineTerminators = new List<int>();
        bool IsWhitespace() => whitespaceCharacters.Contains(Current);

        public Scanner(string content)
        {
            Content = content;
        }

        void Advance(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Position++;
                if (Content[Position] == '\n')
                {
                    _lineTerminators.Add(Position);
                    Line++;
                    Column = 0;
                }
                else Column++;
            }
        }

        DocPos PositionOf(int pos)
        {
            int line = 0;
            int lastLineTerminator = 0;
            for (; line < _lineTerminators.Count && _lineTerminators[line] < pos; line++) lastLineTerminator = _lineTerminators[line];
            int column = pos - lastLineTerminator;
            return new DocPos(line, column);
        }

        bool Is(char character) => Current == character;

        public Token Scan()
        {
            while(true)
            {
                int tokenPos = Position;

                if (IsWhitespace()) Advance();
                else if (Is('=')) return MakeToken(TokenType.Equal, tokenPos, "=");
            }
        }

        Token MakeToken(TokenType type, int pos, string content)
        {
            Advance(content.Length);
            int length = Position - pos;
            return new Token(content, new DocRange(PositionOf(pos), PositionOf(Position)), type);
        }
    }
}