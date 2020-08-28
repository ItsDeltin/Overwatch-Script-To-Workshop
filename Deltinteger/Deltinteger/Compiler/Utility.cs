using System;
using System.Linq;

namespace Deltin.Deltinteger.Compiler
{
    public class DocPos
    {
        public int Line;
        public int Character;

        public DocPos(int line, int character)
        {
            Line = line;
            Character = character;
        }

        public int PosIndex(string text)
        {
            if (Line == 0 && Character == 0) return 0;

            int line = 0;
            int character = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    character = 0;
                }
                else
                {
                    character++;
                }

                if (Line == line && Character == character)
                    return i + 1;
                
                if (line > Line)
                    throw new Exception("Scanned line surpassed expected line.");
            }
            throw new Exception("End reached without encountering position.");
        }

        public override string ToString()
        {
            return Line + ", " + Character;
        }

        public int CompareTo(DocPos other)
        {
            if (other == null || this.Line < other.Line || (this.Line == other.Line && this.Character < other.Character))
                return -1;
            
            if (this.Line == other.Line && this.Character == other.Character)
                return 0;
            
            if (this.Line > other.Line || (this.Line == other.Line && this.Character > other.Character))
                return 1;

            throw new Exception();
        }

        public bool EqualTo(DocPos other) => CompareTo(other) == 0;

        #region Operators
        public static bool operator <(DocPos p1, DocPos p2)  => p1.CompareTo(p2) <  0;
        public static bool operator >(DocPos p1, DocPos p2)  => p1.CompareTo(p2) >  0;
        public static bool operator <=(DocPos p1, DocPos p2) => p1.CompareTo(p2) <= 0;
        public static bool operator >=(DocPos p1, DocPos p2) => p1.CompareTo(p2) >= 0;
        #endregion
    }

    public class DocRange : IComparable<DocRange>
    {
        public DocPos Start;
        public DocPos End;

        public DocRange(DocPos start, DocPos end)
        {
            Start = start;
            End = end;
        }

        public bool IsInside(DocPos pos) => (Start.Line < pos.Line || (Start.Line == pos.Line && pos.Character >= Start.Character))
            && (End.Line > pos.Line || (End.Line == pos.Line && pos.Character <= End.Character));

        public bool DoOverlap(DocRange other) => IsInside(other.Start) || IsInside(other.End);

        public int CompareTo(DocRange other)
        {
            // Return -1 if 'this' is less than 'other'.
            // Return 0 if 'this' is equal to 'other'.
            // Return 1 if 'this' is greater than 'other'.

            // This is greater if other is null.
            if (other == null)
                return 1;

            // Get the number of lines 'start' and 'stop' contain.
            int thisLineDif = this.End.Line - this.Start.Line; 
            int otherLineDif = other.End.Line - other.Start.Line;

            // If 'this' has less lines than 'other', return less than.
            if (thisLineDif < otherLineDif)
                return -1;

            // If 'this' has more lines than 'other', return greater than.
            if (thisLineDif > otherLineDif)
                return 1;
            
            // If the amount of lines are equal, compare by character offset.
            if (thisLineDif == otherLineDif)
            {
                int thisCharDif = this.End.Character - this.Start.Character;
                int otherCharDif = other.End.Character - other.Start.Character;

                // Return less-than.
                if (thisCharDif < otherCharDif)
                    return -1;
                
                // Return equal.
                if (thisCharDif == otherCharDif)
                    return 0;

                // Return greater-than.
                if (thisCharDif > otherCharDif)
                    return 1;
            }

            // This isn't possible.
            throw new Exception();
        }

        public int LineSpan => End.Line - Start.Line;

        public override string ToString() => "[" + Start.ToString() + "] - [" + End.ToString() + "]";

        public static bool operator <(DocRange r1, DocRange r2)  => r1.CompareTo(r2) <  0;
        public static bool operator >(DocRange r1, DocRange r2)  => r1.CompareTo(r2) >  0;
        public static bool operator <=(DocRange r1, DocRange r2) => r1.CompareTo(r2) <= 0;
        public static bool operator >=(DocRange r1, DocRange r2) => r1.CompareTo(r2) >= 0;
    }

    public class Token
    {
        public string Text { get; }
        public int Index { get; set; }
        public int Length { get; }
        public DocRange Range { get; set; }
        public TokenType TokenType { get; }

        public Token(string text, int index, int length, DocRange range, TokenType tokenType)
        {
            Text = text;
            Index = index;
            Length = length;
            Range = range;
            TokenType = tokenType;
        }

        public override string ToString() => "[" + Text + "]";

        public static bool operator true(Token x) => x != null;
        public static bool operator false(Token x) => x == null;
        public static bool operator !(Token x) => x == null;
    }

    public static class TokenExtensions
    {
        public static readonly TokenType[] Assignment_Tokens = new TokenType[] {
            TokenType.Equal, TokenType.AddEqual, TokenType.DivideEqual, TokenType.HatEqual, TokenType.ModuloEqual, TokenType.MultiplyEqual, TokenType.SubtractEqual 
        };

        public static string Name(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Colon: return ":";
                case TokenType.Semicolon: return ";";
                case TokenType.CurlyBracket_Close: return "}";
                case TokenType.CurlyBracket_Open: return "{";
                case TokenType.Parentheses_Close: return ")";
                case TokenType.Parentheses_Open: return "(";
                case TokenType.Dot: return ".";
                case TokenType.Squiggle: return "~";
                case TokenType.SquareBracket_Open: return "[";
                case TokenType.SquareBracket_Close: return "]";
                case TokenType.For: return "for";
                case TokenType.If: return "if";
                case TokenType.Break: return "break";
                case TokenType.Continue: return "continue";
                case TokenType.Identifier: return "identifier";
                case TokenType.String: return "string";
                case TokenType.Number: return "number";
                case TokenType.True: return "true";
                case TokenType.False: return "false";
                case TokenType.Unknown: return "unknown";
                default: return tokenType.ToString().ToLower();
            }
        }

        public static bool IsAssignmentOperator(this TokenType tokenType) => Assignment_Tokens.Contains(tokenType);
    }

    public enum TokenType
    {
        Unknown,
        Identifier,
        // Pair symbols
        CurlyBracket_Open,
        CurlyBracket_Close,
        Parentheses_Open,
        Parentheses_Close,
        SquareBracket_Open,
        SquareBracket_Close,
        // Symbols
        Colon,
        Semicolon,
        Dot,
        Squiggle,
        Exclamation,
        Comma,
        // Assignment
        Equal,
        HatEqual,
        MultiplyEqual,
        DivideEqual,
        ModuloEqual,
        AddEqual,
        SubtractEqual,
        // Math
        Hat,
        Multiply,
        Divide,
        Modulo,
        Add,
        Subtract,
        // Boolean
        And,
        Or,
        // Generic expressions
        String,
        Number,
        True,
        False,
        // Keywords
        Define,
        Break,
        Continue,
        Rule,
        For,
        If,
        // Comparison
        NotEqual,
        EqualEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        // Ternary
        QuestionMark
    }

    public class UpdateRange
    {
        public DocRange Range { get; }
        public string Text { get; }
    }
}