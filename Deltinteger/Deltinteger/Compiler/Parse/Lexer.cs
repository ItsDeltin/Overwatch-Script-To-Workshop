using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class Lexer
    {
        public List<Token> Tokens { get; } = new List<Token>();
        public string Content { get; private set; }
        public List<int> Newlines { get; } = new List<int>();

        public Lexer() {}

        public void Init(string content)
        {
            Content = content;
            GetNewlines(Content); // This can be removed after debugging.

            LexController controller = new LexController(Content, new InitTokenPush(Tokens));
            controller.Match();
        }

        public void Reset()
        {
            Tokens.Clear();
            Content = null;
            Newlines.Clear();
        }

        public string DebugTokens()
        {
            StringBuilder debugString = new StringBuilder();
            foreach (Token token in Tokens)
                debugString.AppendLine(token.TokenType.ToString() + ": " + token.Range.ToString() + " '" + token.Text + "'");
            
            return debugString.ToString();
        }

        public IncrementInfo Update(string newContent, UpdateRange updateRange)
        {
            int lastTokenCount = Tokens.Count;
            AffectedAreaInfo affectedArea = GetAffectedArea(updateRange);
            
            // The number of lines
            int lineDelta = NumberOfNewLines(updateRange.Text) - updateRange.Range.LineSpan();
            int columnDelta = NumberOfCharactersInLastLine(updateRange.Text) - updateRange.Range.ColumnSpan();

            var tokenInsert = new IncrementalTokenInsert(Tokens, affectedArea.StartingTokenIndex, affectedArea.EndingTokenIndex);
            LexController controller = new LexController(newContent, tokenInsert);

            // Set start range
            controller.Index = affectedArea.StartIndex;
            controller.Line = GetLine(controller.Index);
            controller.Column = GetColumn(controller.Index);

            // Adjust token ranges.
            for (int i = affectedArea.EndingTokenIndex; i < Tokens.Count; i++)
            {
                if (updateRange.Range.End <= Tokens[i].Range.Start)
                {
                    Tokens[i].Range.Start.Line += lineDelta;
                    Tokens[i].Range.End.Line += lineDelta;

                    if (updateRange.Range.End.Line == Tokens[i].Range.Start.Line)
                    {
                        Tokens[i].Range.Start.Character += columnDelta;
                        Tokens[i].Range.End.Character += columnDelta;
                    }
                }
            }

            controller.Match();

            Content = newContent;
            GetNewlines(newContent);
            return new IncrementInfo(affectedArea.StartingTokenIndex, affectedArea.EndingTokenIndex, Tokens.Count - lastTokenCount);
        }

        AffectedAreaInfo GetAffectedArea(UpdateRange updateRange)
        {
            // Get the range of the tokens overlapped.
            bool startSet = false;

            // The default starting range of the update range's start and end positions in the document.
            int updateStartIndex = IndexOf(updateRange.Range.Start);
            int updateEndIndex = IndexOf(updateRange.Range.End);

            int startIndex = updateStartIndex; // The position where lexing will start.
            int startingTokenIndex = Tokens.Count; // The position in the token list where new tokens will be inserted into.
            int endingTokenIndex = int.MaxValue; // The token where lexing will end.

            // Find the first token to the left of the update range and the first token to the right of the update range.
            // Set 'startIndex', 'startingTokenIndex' with the left token and 'endingTokenIndex' with the right token.
            // 
            // In the event of a left-side or right-side token in relation to the update range is not found,
            // the default values of 'startIndex', 'startingTokenIndex', and 'endingTokenIndex' should handle it fine.
            for (int i = 0; i < Tokens.Count; i++)
            {
                // If the current token overlaps the update range, set startTokenIndex.
                if (Tokens[i].Range.DoOverlap(updateRange.Range))
                {
                    // Don't set the starting position and index again if it was already set via overlap.
                    // We use a seperate if rather than an && because the proceeding else-ifs cannot run if the token overlaps the update range.
                    if (!startSet)
                    {
                        // 'startIndex' cannot be higher than 'updateStartIndex'.
                        // Simply setting 'startIndex' to 'IndexOf...' may cause an issue in the common scenario:

                        // Character            : |1|2|3|4|5|6|7|
                        // Update start position:    x     x
                        // Token start position :        +     +

                        // startIndex will be 3, causing the characters between the first x and + to be skipped.
                        // So pick the lower of the 2 values.
                        startIndex = Math.Min(updateStartIndex, IndexOf(Tokens[i].Range.Start));
                        // Set the starting token to the current token's index.
                        startingTokenIndex = i;
                        // Once an overlapping token is found, do not set 'startIndex' or 'startingTokenIndex' again.
                        startSet = true;
                    }
                }
                // Sometimes, there is no overlapping token. In that case, we use the closest token to the left of the update range.
                // We determine if a token is to the left by checking if the token's ending position is less than the update range's start position.
                //
                // If the overlapping token is not found,
                //   and the token is to the left of the update range,
                //   then set the 'startIndex' and 'startingTokenIndex'.
                //
                // This block will run every iteration until the if statement above executes.
                else if (!startSet && Tokens[i].Range.End < updateRange.Range.Start)
                {
                    // TODO: Since the end position of the token was already checked, doing Math.Min is probably redundant
                    // simply doing 'startIndex = IndexOf(Tokens[i].Range.Start)' will probably suffice.
                    startIndex = Math.Min(updateStartIndex, IndexOf(Tokens[i].Range.Start));
                    // Set the starting token to the current token's index.
                    startingTokenIndex = i;
                }
                // If the token was overlapping, it would have been caught earlier.
                // This block will run once no more overlapping tokens are found.
                else if (startSet)
                {
                    // Subtract by 1 because i - 1 is the last token that overlaps with the update range.
                    endingTokenIndex = i - 1;

                    // No more iterations are need once the end is found.
                    break;
                }
                // If there is no overlapping token, set the ending using the first token that is completely to the right of the update range.
                else if (!startSet && updateRange.Range.End < Tokens[i].Range.Start)
                {
                    // Set the token index where lexing will stop.
                    endingTokenIndex = i;

                    // No more iterations are need once the end is found.
                    break;
                }
            }

            return new AffectedAreaInfo(startIndex, startingTokenIndex, endingTokenIndex);
        }

        private void GetNewlines(string content)
        {
            Newlines.Clear();
            for (int i = 0; i < content.Length; i++)
                if (content[i] == '\n')
                    Newlines.Add(i);
        }

        private int GetLine(int index)
        {
            int r;
            for (r = 0; r < Newlines.Count && Newlines[r] < index; r++);
            return r;
        }
        // private int GetColumn(int index) => Newlines.Count == 0 ? index : index - GetLineIndex(GetLine(index));
        private int GetColumn(int index) => index - GetLineIndex(GetLine(index));
        private int IndexOf(DocPos pos) => GetLineIndex(pos.Line) + pos.Character;
        private int GetLineIndex(int line) => line == 0 ? 0 : (Newlines[line - 1] + 1);

        private static int NumberOfNewLines(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    count++;
            return count;
        }

        private static int NumberOfCharactersInLastLine(string text) => text.Split('\n').Last().Length;

        public Token NextToken(Token token) => Tokens[Tokens.IndexOf(token) + 1];
    }

    public class LexController
    {
        public int Index;
        public int Line;
        public int Column;
        public string Content { get; }
        private readonly ITokenPush _push;

        public LexController(string content, ITokenPush push)
        {
            Content = content;
            _push = push;
        }

        public void Match()
        {
            while (Index < Content.Length && !_push.IncrementalStop())
            {
                Skip();
                if (Index >= Content.Length) break;

                bool matched =
                    MatchComment() ||
                    MatchSymbol('{', TokenType.CurlyBracket_Open) ||
                    MatchSymbol('}', TokenType.CurlyBracket_Close) ||
                    MatchSymbol('(', TokenType.Parentheses_Open) ||
                    MatchSymbol(')', TokenType.Parentheses_Close) ||
                    MatchSymbol('[', TokenType.SquareBracket_Open) ||
                    MatchSymbol(']', TokenType.SquareBracket_Close) ||
                    MatchSymbol(':', TokenType.Colon) ||
                    MatchSymbol('?', TokenType.QuestionMark) ||
                    MatchSymbol(';', TokenType.Semicolon) ||
                    MatchSymbol('.', TokenType.Dot) ||
                    MatchSymbol('~', TokenType.Squiggle) ||
                    MatchSymbol("=>", TokenType.Arrow) ||
                    MatchSymbol("!=", TokenType.NotEqual) ||
                    MatchSymbol("==", TokenType.EqualEqual) ||
                    MatchSymbol("<=", TokenType.LessThanOrEqual) ||
                    MatchSymbol(">=", TokenType.GreaterThanOrEqual) ||
                    MatchSymbol('!', TokenType.Exclamation) ||
                    MatchSymbol("^=", TokenType.HatEqual) ||
                    MatchSymbol("*=", TokenType.MultiplyEqual) ||
                    MatchSymbol("/=", TokenType.DivideEqual) ||
                    MatchSymbol("%=", TokenType.ModuloEqual) ||
                    MatchSymbol("+=", TokenType.AddEqual) ||
                    MatchSymbol("-=", TokenType.SubtractEqual) ||
                    MatchSymbol('=', TokenType.Equal) ||
                    MatchSymbol('<', TokenType.LessThan) ||
                    MatchSymbol('>', TokenType.GreaterThan) ||
                    MatchSymbol(',', TokenType.Comma) ||
                    MatchSymbol('^', TokenType.Hat) ||
                    MatchSymbol('*', TokenType.Multiply) ||
                    MatchSymbol('/', TokenType.Divide) ||
                    MatchSymbol('%', TokenType.Modulo) ||
                    MatchSymbol("++", TokenType.PlusPlus) ||
                    MatchSymbol('+', TokenType.Add) ||
                    MatchSymbol("--", TokenType.MinusMinus) ||
                    MatchSymbol('-', TokenType.Subtract) ||
                    MatchSymbol("&&", TokenType.And) ||
                    MatchSymbol("||", TokenType.Or) ||
                    MatchSymbol('@', TokenType.At) ||
                    MatchKeyword("import", TokenType.Import) ||
                    MatchKeyword("for", TokenType.For) ||
                    MatchKeyword("while", TokenType.While) ||
                    MatchKeyword("foreach", TokenType.Foreach) ||
                    MatchKeyword("in", TokenType.In) ||
                    MatchKeyword("rule", TokenType.Rule) ||
                    MatchKeyword("disabled", TokenType.Disabled) ||
                    MatchKeyword("true", TokenType.True) ||
                    MatchKeyword("false", TokenType.False) ||
                    MatchKeyword("null", TokenType.Null) ||
                    MatchKeyword("if", TokenType.If) ||
                    MatchKeyword("else", TokenType.Else) ||
                    MatchKeyword("break", TokenType.Break) ||
                    MatchKeyword("continue", TokenType.Continue) ||
                    MatchKeyword("switch", TokenType.Switch) ||
                    MatchKeyword("case", TokenType.Case) ||
                    MatchKeyword("default", TokenType.Default) ||
                    MatchKeyword("class", TokenType.Class) ||
                    MatchKeyword("enum", TokenType.Enum) ||
                    MatchKeyword("new", TokenType.New) ||
                    MatchKeyword("delete", TokenType.Delete) ||
                    MatchKeyword("define", TokenType.Define) ||
                    MatchKeyword("void", TokenType.Void) ||
                    MatchKeyword("public", TokenType.Public) ||
                    MatchKeyword("private", TokenType.Private) ||
                    MatchKeyword("protected", TokenType.Protected) ||
                    MatchKeyword("static", TokenType.Static) ||
                    MatchKeyword("override", TokenType.Override) ||
                    MatchKeyword("virtual", TokenType.Virtual) ||
                    MatchKeyword("recursive", TokenType.Recursive) ||
                    MatchKeyword("globalvar", TokenType.GlobalVar) ||
                    MatchKeyword("playervar", TokenType.PlayerVar) ||
                    MatchKeyword("ref", TokenType.Ref) ||
                    MatchKeyword("this", TokenType.This) ||
                    MatchKeyword("as", TokenType.As) ||
                    MatchNumber() ||
                    MatchIdentifier() ||
                    MatchString();
                
                if (!matched)
                    Unknown();
            }
            if (Index >= Content.Length) _push.EndReached();
        }

        private LexScanner MakeScanner() => new LexScanner(this);

        private void Accept(LexScanner scanner)
        {
            Index = scanner.Index;
            Line = scanner.Line;
            Column = scanner.Column;
        }
        
        // * Matchers *

        /// <summary>Matches a keyword.</summary>
        /// <param name="keyword">The name of the keyword that will be matched.</param>
        /// <param name="tokenType">The type of the created token.</param>
        /// <returns>Wether the keyword was matched.</returns>
        public bool MatchKeyword(string keyword, TokenType tokenType)
        {
            LexScanner scanner = MakeScanner();
            if (scanner.Match(keyword) && !scanner.AtIdentifierChar())
            {
                PushToken(scanner.AsToken(tokenType));
                Accept(scanner);
                return true;
            }
            return false;
        }

        /// <summary>Matches a symbol.</summary>
        /// <returns>Wether a symbol was matched.</returns>
        public bool MatchSymbol(string symbol, TokenType tokenType)
        {
            LexScanner scanner = MakeScanner();
            if (scanner.Match(symbol))
            {
                PushToken(scanner.AsToken(tokenType));
                Accept(scanner);
                return true;
            }
            return false;
        }

        /// <summary>Matches a symbol.</summary>
        /// <returns>Wether a symbol was matched.</returns>
        public bool MatchSymbol(char symbol, TokenType tokenType)
        {
            LexScanner scanner = MakeScanner();
            if (scanner.Match(symbol))
            {
                PushToken(scanner.AsToken(tokenType));
                Accept(scanner);
                return true;
            }
            return false;
        }

        /// <summary>Matches an identifier.</summary>
        /// <returns>Wether an identifier was matched.</returns>
        public bool MatchIdentifier()
        {
            LexScanner scanner = MakeScanner();
            
            // Advance while the current character is an identifier.
            while (!scanner.ReachedEnd && scanner.AtIdentifierChar()) scanner.Advance();

            // Push the token if it is accepted.
            if (scanner.WasAdvanced)
            {
                PushToken(scanner.AsToken(TokenType.Identifier));
                Accept(scanner);
                return true;
            }
            return false;
        }

        /// <summary>Matches a string. Works with single or double quotes and escaping.</summary>
        /// <returns>Wether a string was matched.</returns>
        public bool MatchString()
        {
            LexScanner scanner = MakeScanner();

            // single will be true for single quotes, false for double quotes.
            bool single = scanner.At('\'');

            // Not a string.
            if (!single && !scanner.At('\"')) return false;

            char lookingFor = single ? '\'' : '\"';

            bool escaped = false;
            // Look for end of string.
            do
            {
                scanner.Advance();
                if (scanner.At('\\') && !escaped) escaped = true;
                else if (escaped) escaped = false;
            }
            while (!scanner.ReachedEnd && (escaped || !scanner.At(lookingFor)));
            scanner.Advance();

            PushToken(scanner, TokenType.String);
            return true;
        }

        /// <summary>Matches a number.</summary>
        /// <returns>Wether a number was matched.</returns>
        public bool MatchNumber()
        {
            LexScanner scanner = MakeScanner();

            // Capture -
            if (scanner.At('-'))
                scanner.Advance();

            // Get the number.
            bool foundLeftNumber = false;
            while (scanner.AtNumeric())
            {
                scanner.Advance();
                foundLeftNumber = true;
            }

            // At decimal
            if (scanner.At('.'))
            {
                // Get the decimal.
                bool decimalFound = false;
                scanner.Advance();
                while (scanner.AtNumeric())
                {
                    scanner.Advance();
                    decimalFound = true;
                }

                if (!decimalFound)
                    return false;
            }
            // No decimal and no left number.
            else if (!foundLeftNumber)
                return false;

            // Done.
            PushToken(scanner, TokenType.Number);
            return true;
        }

        bool MatchComment()
        {
            LexScanner scanner = MakeScanner();
            // Action comment.
            if (!scanner.At('#')) return false;

            // Match every character to the end of the line.
            scanner.Advance();
            while(!scanner.At('\n')) scanner.Advance();
            
            // Done.
            PushToken(scanner, TokenType.ActionComment);
            return true;
        }

        /// <summary>Unknown token.</summary>
        public void Unknown()
        {
            LexScanner scanner = MakeScanner();
            PushToken(new Token(Content[Index].ToString(), new DocRange(new DocPos(Line, Column), new DocPos(Line, Column + 1)), TokenType.Unknown));
            scanner.Advance();
            Accept(scanner);
        }

        /// <summary>Skips whitespace.</summary>
        public bool Skip()
        {
            bool preceedingWhitespace = false;
            LexScanner scanner = MakeScanner();
            while (!scanner.ReachedEnd && scanner.AtWhitespace())
            {
                if (scanner.At('\n')) preceedingWhitespace = true;
                scanner.Advance();
            }
            Accept(scanner);
            return preceedingWhitespace;
        }

        /// <summary>Pushes a token to the token list.</summary>
        /// <param name="token">The token that is pushed.</param>
        private void PushToken(Token token)
        {
            _push.PushToken(token);
        }

        private void PushToken(LexScanner scanner, TokenType tokenType)
        {
            _push.PushToken(scanner.AsToken(tokenType));
            Accept(scanner);
        }
    }

    public class LexScanner
    {
        private static readonly char[] identifierCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();
        private static readonly char[] numericalCharacters = "0123456789".ToCharArray();
        private static readonly char[] whitespaceCharacters = " \t\r\n".ToCharArray();
        public int Index { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public bool WasAdvanced { get; private set; }
        public bool ReachedEnd => Index >= _content.Length;
        private readonly string _content;
        private readonly StringBuilder _captured = new StringBuilder();
        private readonly DocPos _startPos;
        
        public LexScanner(LexController controller)
        {
            Line = controller.Line;
            Column = controller.Column;
            Index = controller.Index;
            _content = controller.Content;
            _startPos = new DocPos(Line, Column);
        }

        public void Advance()
        {
            if (ReachedEnd) return;

            _captured.Append(_content[Index]);
            WasAdvanced = true;
            if (_content[Index] == '\n')
            {
                Line++;
                Column = 0;
            }
            else Column++;
            Index++;
        }

        public bool Match(string content)
        {
            for (int i = 0; i < content.Length; i++)
                if (!Match(content[i]))
                    return false;
            return true;
        }

        public bool Match(char character)
        {
            bool res = !ReachedEnd && _content[Index] == character;
            Advance();
            return res;
        }

        public bool At(char chr) => !ReachedEnd && _content[Index] == chr;
        public bool AtIdentifierChar() => !ReachedEnd && identifierCharacters.Contains(_content[Index]);
        public bool AtWhitespace() => !ReachedEnd && whitespaceCharacters.Contains(_content[Index]);
        public bool AtNumeric() => !ReachedEnd && numericalCharacters.Contains(_content[Index]);
        public Token AsToken(TokenType tokenType) => new Token(_captured.ToString(), new DocRange(_startPos, new DocPos(Line, Column)), tokenType);
    }

    public interface ITokenPush
    {
        void PushToken(Token token);
        bool IncrementalStop();
        void EndReached();
    }

    class InitTokenPush : ITokenPush
    {
        private readonly List<Token> _tokens;

        public InitTokenPush(List<Token> list)
        {
            _tokens = list;
        }

        public void PushToken(Token token) => _tokens.Add(token);
        public bool IncrementalStop() => false;
        public void EndReached() {}
    }

    class IncrementalTokenInsert : ITokenPush
    {
        public int Current { get; private set; }
        private readonly List<Token> _tokens;
        private bool _lastInsertWasEqual;
        private int _stopMinimum;

        public IncrementalTokenInsert(List<Token> list, int startingTokenIndex, int stopMinimum)
        {
            Current = startingTokenIndex;
            _tokens = list;
            _stopMinimum = stopMinimum;
        }
        public void PushToken(Token token)
        {
            _lastInsertWasEqual = Current < _tokens.Count && _tokens[Current].TokenType == token.TokenType && _tokens[Current].Text == token.Text;
            if (Current == _tokens.Count)
                _tokens.Add(token);
            else
            {
                _tokens.RemoveAt(Current);
                _tokens.Insert(Current, token);
            }
            Current++;
        }

        public bool IncrementalStop() => Current > _stopMinimum && _lastInsertWasEqual;

        public void EndReached()
        {
            while (_tokens.Count > Current)
                _tokens.RemoveAt(Current);
        }
    }

    class AffectedAreaInfo
    {
        public int StartIndex { get; }
        public int StartingTokenIndex { get; }
        public int EndingTokenIndex { get; }

        public AffectedAreaInfo(int startIndex, int startingTokenIndex, int endingTokenIndex)
        {
            StartIndex = startIndex;
            StartingTokenIndex = startingTokenIndex;
            EndingTokenIndex = endingTokenIndex;
        }
    }

    public class IncrementInfo
    {
        public int ChangeStart { get; }
        public int ChangeEnd { get; }
        public int Delta { get; }

        public IncrementInfo(int startIgnoring, int stopIgnoring, int delta)
        {
            ChangeStart = startIgnoring;
            ChangeEnd = stopIgnoring;
            Delta = delta;
        }
    }
}