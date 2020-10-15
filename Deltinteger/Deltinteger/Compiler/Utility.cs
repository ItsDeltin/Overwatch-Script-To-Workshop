using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LSPos   = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LSRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Deltin.Deltinteger.Compiler
{
    public class DocPos : IComparable<DocPos>
    {
        public static DocPos Zero = new DocPos(0, 0);

        [JsonProperty("line")] 
        public int Line;
        [JsonProperty("character")] 
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

        public override bool Equals(object obj) => obj is DocPos pos &&
            Line == pos.Line &&
            Character == pos.Character;

        public override int GetHashCode() => HashCode.Combine(Line, Character);

        public static bool operator <(DocPos p1, DocPos p2)  => p1.CompareTo(p2) <  0;
        public static bool operator >(DocPos p1, DocPos p2)  => p1.CompareTo(p2) >  0;
        public static bool operator <=(DocPos p1, DocPos p2) => p1.CompareTo(p2) <= 0;
        public static bool operator >=(DocPos p1, DocPos p2) => p1.CompareTo(p2) >= 0;
        public static DocRange operator +(DocPos p1, DocPos p2) => new DocRange(p1, p2);
        public static implicit operator DocPos(OmniSharp.Extensions.LanguageServer.Protocol.Models.Position pos) => new DocPos(pos.Line, pos.Character);
        public static implicit operator LSPos(DocPos pos) => new LSPos(pos.Line, pos.Character);
    }

    public class DocRange : IComparable<DocRange>
    {
        public static readonly DocRange Zero = new DocRange(DocPos.Zero, DocPos.Zero);

        [JsonProperty("start")] 
        public DocPos Start;
        [JsonProperty("end")] 
        public DocPos End;

        public DocRange(DocPos start, DocPos end)
        {
            Start = start;
            End = end;
        }

        public bool IsInside(DocPos pos) => (Start.Line < pos.Line || (Start.Line == pos.Line && pos.Character >= Start.Character))
            && (End.Line > pos.Line || (End.Line == pos.Line && pos.Character <= End.Character));

        public bool DoOverlap(DocRange other) => IsInside(other.Start) || IsInside(other.End) || other.IsInside(Start) || other.IsInside(End);

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

        public int LineSpan() => End.Line - Start.Line;
        public int ColumnSpan()
        {
            int span = End.Character;
            if (Start.Line == End.Line)
                span -= Start.Character;
            return span;
        }

        public override string ToString() => "[" + Start.ToString() + "] - [" + End.ToString() + "]";

        public override bool Equals(object obj) => obj is DocRange range &&
            EqualityComparer<DocPos>.Default.Equals(Start, range.Start) &&
            EqualityComparer<DocPos>.Default.Equals(End, range.End);

        public override int GetHashCode() => HashCode.Combine(Start, End);

        public static bool operator <(DocRange r1, DocRange r2)  => r1.CompareTo(r2) <  0;
        public static bool operator >(DocRange r1, DocRange r2)  => r1.CompareTo(r2) >  0;
        public static bool operator <=(DocRange r1, DocRange r2) => r1.CompareTo(r2) <= 0;
        public static bool operator >=(DocRange r1, DocRange r2) => r1.CompareTo(r2) >= 0;
        public static DocRange operator +(DocRange start, DocRange end) => new DocRange(start.Start, end.End);
        public static implicit operator DocRange(OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range) => new DocRange(range.Start, range.End);
        public static implicit operator LSRange(DocRange range) => new LSRange(range.Start, range.End);
    }

    public class Token
    {
        public string Text { get; }
        public DocRange Range { get; set; }
        public TokenType TokenType { get; }

        public Token(string text, DocRange range, TokenType tokenType)
        {
            Text = text;
            Range = range;
            TokenType = tokenType;
        }

        public override string ToString() => "[" + Text + "]";

        public static bool operator true(Token x) => x != null;
        public static bool operator false(Token x) => x == null;
        public static bool operator !(Token x) => x == null;
        public static implicit operator bool(Token x) => x != null;
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
                case TokenType.CurlyBracket_Close: return "}";
                case TokenType.CurlyBracket_Open: return "{";
                case TokenType.Parentheses_Close: return ")";
                case TokenType.Parentheses_Open: return "(";
                case TokenType.SquareBracket_Open: return "[";
                case TokenType.SquareBracket_Close: return "]";
                case TokenType.Colon: return ":";
                case TokenType.Semicolon: return ";";
                case TokenType.Dot: return ".";
                case TokenType.Squiggle: return "~";
                case TokenType.Exclamation: return "!";
                case TokenType.Comma: return ",";
                case TokenType.Arrow: return "=>";
                case TokenType.At: return "@";
                case TokenType.Equal: return "=";
                case TokenType.HatEqual: return "^=";
                case TokenType.MultiplyEqual: return "*=";
                case TokenType.ModuloEqual: return "%=";
                case TokenType.AddEqual: return "+=";
                case TokenType.SubtractEqual: return "-=";
                case TokenType.Hat: return "^";
                case TokenType.Multiply: return "*";
                case TokenType.Divide: return "/";
                case TokenType.Modulo: return "%";
                case TokenType.Add: return "+";
                case TokenType.Subtract: return "-";
                case TokenType.PlusPlus: return "++";
                case TokenType.MinusMinus: return "--";
                case TokenType.And: return "&&";
                case TokenType.Or: return "||";
                case TokenType.NotEqual: return "!=";
                case TokenType.EqualEqual: return "==";
                case TokenType.LessThan: return "<";
                case TokenType.GreaterThan: return ">";
                case TokenType.LessThanOrEqual: return "<=";
                case TokenType.GreaterThanOrEqual: return ">=";
                case TokenType.QuestionMark: return "?";
                case TokenType.For: return "for";
                case TokenType.If: return "if";
                case TokenType.Break: return "break";
                case TokenType.Continue: return "continue";
                case TokenType.Identifier: return "identifier";
                case TokenType.String: return "string";
                case TokenType.Number: return "number";
                case TokenType.True: return "true";
                case TokenType.False: return "false";
				case TokenType.Operator: return "operator";
                case TokenType.Unknown: return "unknown";
                default: return tokenType.ToString().ToLower();
            }
        }

        public static bool IsSkippable(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Rule:
                case TokenType.Class:
                case TokenType.Enum:
                case TokenType.Public:
                case TokenType.Protected:
                case TokenType.Private:
                case TokenType.Override:
                case TokenType.Virtual:
                case TokenType.Void:
                case TokenType.Recursive:
                case TokenType.PlayerVar:
                case TokenType.GlobalVar:
                case TokenType.Static:
                case TokenType.SquareBracket_Close:
                case TokenType.SquareBracket_Open:
                case TokenType.Parentheses_Close:
                case TokenType.Parentheses_Open:
                case TokenType.CurlyBracket_Close:
                case TokenType.CurlyBracket_Open:
				case TokenType.Operator:
                case TokenType.EOF:
                    return false;
                
                default:
                    return true;
            }
        }

        public static bool IsStartOfExpression(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.False:
                case TokenType.Identifier:
                case TokenType.New:
                case TokenType.Null:
                case TokenType.Number:
                case TokenType.True:
                case TokenType.This:
                case TokenType.Root:
                case TokenType.String:
                case TokenType.Parentheses_Open:
                case TokenType.SquareBracket_Open:
                // Unary
                case TokenType.Subtract:
                case TokenType.Exclamation:
                // Type cast
                case TokenType.LessThan:
                    return true;
                
                default:
                    return tokenType.IsStartOfType(); // Lambdas
            }
        }

        public static bool IsBinaryOperator(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Add:
                case TokenType.And:
                case TokenType.Divide:
                case TokenType.Dot:
                case TokenType.Equal:
                case TokenType.GreaterThan:
                case TokenType.GreaterThanOrEqual:
                case TokenType.Hat:
                case TokenType.LessThan:
                case TokenType.LessThanOrEqual:
                case TokenType.Modulo:
                case TokenType.Multiply:
                case TokenType.NotEqual:
                case TokenType.Or:
                case TokenType.QuestionMark:
                case TokenType.Squiggle:
                case TokenType.Subtract:
                    return true;
                
                default:
                    return false;
            }
        }

        public static bool IsStartOfType(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Parentheses_Open:
                case TokenType.Identifier:
                case TokenType.Define:
                case TokenType.Void:
                    return true;
                
                default:
                    return false;
            }
        }

        public static bool IsStartOfStatement(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Break:
                case TokenType.Case:
                case TokenType.Continue:
                case TokenType.CurlyBracket_Open:
                case TokenType.Default:
                case TokenType.Delete:
                case TokenType.Else:
                case TokenType.For:
                case TokenType.Foreach:
                case TokenType.If:
                case TokenType.New:
                case TokenType.Return:
                case TokenType.Semicolon:
                case TokenType.Switch:
                case TokenType.While:
                    return true;
                
                default:
                    return tokenType.IsStartOfType() || tokenType.IsStartOfExpression();
            }
        }

        public static bool IsAssignmentOperator(this TokenType tokenType) => Assignment_Tokens.Contains(tokenType);

        /// <summary>Gets the token's text. If the token is null, "?" is returned.</summary>
        public static string GetText(this Token token) => token ? token.Text : "?";

        /// <summary>Gets the token's range. If the token is null, the fallback is used.</summary>
        public static DocRange GetRange(this Token token, DocRange fallback) => token ? token.Range : fallback;
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
        Arrow,
        At,
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
        // Increment/Decrement
        PlusPlus,
        MinusMinus,
        // Boolean
        And,
        Or,
        Pipe,
        // Generic expressions
        String,
        Number,
        True,
        False,
        Null,
        // Keywords
        Import,
        Define,
        Break,
        Continue,
        Return,
        Rule,
        Disabled,
        For,
        While,
        Foreach,
        In,
        If,
        Else,
        Switch,
        Case,
        Default,
        Class,
        Enum,
        Void,
        New,
        Delete,
        This,
        Root,
        As,
		Operator,
        // Attributes
        Public,
        Private,
        Protected,
        Static,
        Override,
        Virtual,
        Recursive,
        GlobalVar,
        PlayerVar,
        Ref,
        // Comparison
        NotEqual,
        EqualEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        // Ternary
        QuestionMark,
        // Other
        ActionComment,
        EOF
    }

    public class UpdateRange
    {
        public DocRange Range { get; }
        public string Text { get; }

        public UpdateRange(DocRange range, string text)
        {
            Range = range;
            Text = text;
        }

        public static implicit operator UpdateRange(OmniSharp.Extensions.LanguageServer.Protocol.Models.TextDocumentContentChangeEvent e) => new UpdateRange(e.Range, e.Text);
    }
}
