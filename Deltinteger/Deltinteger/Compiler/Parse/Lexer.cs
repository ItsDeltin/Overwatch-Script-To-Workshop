using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler
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

        public string DebugTokens()
        {
            StringBuilder debugString = new StringBuilder();
            foreach (Token token in Tokens)
                debugString.AppendLine(token.TokenType.ToString() + ": " + token.Range.ToString() + " '" + token.Text + "'");
            
            return debugString.ToString();
        }

        // TODO
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
            return new IncrementInfo(affectedArea.StartIndex, affectedArea.EndIndex, Tokens.Count - lastTokenCount);
        }

        AffectedAreaInfo GetAffectedArea(UpdateRange updateRange)
        {
            // Get the range of the tokens overlapped.
            bool startSet = false;

            int updateStartIndex = IndexOf(updateRange.Range.Start);
            int updateEndIndex = IndexOf(updateRange.Range.End);

            int startIndex = updateStartIndex;
            int startingTokenIndex = Tokens.Count;
            int endIndex = updateEndIndex;
            int endingTokenIndex = int.MaxValue;

            for (int i = 0; i < Tokens.Count; i++)
            {
                if (Tokens[i].Range.DoOverlap(updateRange.Range))
                {
                    if (!startSet)
                    {
                        startIndex = Math.Min(updateStartIndex, IndexOf(Tokens[i].Range.Start));
                        startingTokenIndex = i;
                        startSet = true;
                    }
                }
                else if (!startSet && Tokens[i].Range.End < updateRange.Range.Start)
                {
                    startIndex = Math.Min(updateStartIndex, IndexOf(Tokens[i].Range.Start));
                    startingTokenIndex = i;
                }
                else if (startSet)
                {
                    // End range.
                    endIndex = Math.Max(updateEndIndex, IndexOf(Tokens[i - 1].Range.End));
                    endingTokenIndex = i - 1;
                    break;
                }
                else if (!startSet && updateRange.Range.End < Tokens[i].Range.Start)
                {
                    // End range.
                    endIndex = Math.Max(updateEndIndex, IndexOf(Tokens[i].Range.End));
                    endingTokenIndex = i;
                    break;
                }
            }
            
            return new AffectedAreaInfo(startIndex, endIndex, startingTokenIndex, endingTokenIndex);
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
                    MatchKeyword("for", TokenType.For) ||
                    MatchKeyword("rule", TokenType.Rule) ||
                    MatchKeyword("true", TokenType.True) ||
                    MatchKeyword("false", TokenType.False) ||
                    MatchKeyword("if", TokenType.If) ||
                    MatchKeyword("else", TokenType.Else) ||
                    MatchKeyword("break", TokenType.Break) ||
                    MatchKeyword("continue", TokenType.Continue) ||
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
        private static readonly char[] whitespaceCharacters = " \r\n".ToCharArray();
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
        public int EndIndex { get; }
        public int StartingTokenIndex { get; }
        public int EndingTokenIndex { get; }

        public AffectedAreaInfo(int startIndex, int endIndex, int startingTokenIndex, int endingTokenIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
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