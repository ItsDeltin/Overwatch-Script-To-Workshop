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

        public Lexer() {}

        public void Init(string content)
        {
            Content = content;

            LexController controller = new LexController(Content, Tokens);
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
        public void Update(string newContent, UpdateRange updateRange)
        {
            // Get the range of the tokens overlapped.
            int start = -1;
            int end = Tokens.Count - 1;
            for (int i = 0; i < Tokens.Count; i++)
                if (Tokens[i].Range.DoOverlap(updateRange.Range))
                {
                    start = i;
                }
                else if (start != -1)
                {
                    // End range.
                    end = i;
                    break;
                }
            
            // start shouldn't be -1 anymore.
            if (start == -1) throw new Exception("Could not find the start of the tokens in range of the updated content.");

            // Adjust start if the previous token is touching the update range.
            if (start != 0 && Tokens[start - 1].Range.End.EqualTo(updateRange.Range.Start))
                start--;
            
            // Adjust end if the next token is touching the update range.
            if (end < Tokens.Count - 1 && Tokens[end + 1].Range.Start.EqualTo(updateRange.Range.End))
                end++;

            // Get the content scan range.
            int startScan = Tokens[start].Index;
            int endScan = Tokens[end].Index + Tokens[end].Length;

            // The number of lines
            int lineDifference = updateRange.Range.LineSpan - NumberOfNewLines(updateRange.Text);
            int indexDifference = newContent.Length - (updateRange.Range.End.PosIndex(Content) - updateRange.Range.Start.PosIndex(Content));

            // Adjust token ranges.
            foreach (Token token in Tokens)
                if (updateRange.Range.End < token.Range.Start)
                {
                    token.Index += indexDifference;
                    token.Range.Start.Line += lineDifference;
                    token.Range.End.Line += lineDifference;
                }

            // Remove tokens.
            for (int i = start; i < start + end; i++) Tokens.RemoveAt(i);
        }

        private static int NumberOfNewLines(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    count++;
            return count;
        }

        public Token NextToken(Token token) => Tokens[Tokens.IndexOf(token) + 1];
    }

    public class LexController
    {
        public int Index;
        public int Line;
        public int Column;
        public string Content { get; }
        private readonly List<Token> _push;

        public LexController(string content, List<Token> push)
        {
            Content = content;
            _push = push;
        }

        public void Match()
        {
            while (Index != Content.Length)
            {
                Skip();
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
                    MatchSymbol("++", TokenType.MinusMinus) ||
                    MatchSymbol('-', TokenType.Subtract) ||
                    MatchSymbol("&&", TokenType.And) ||
                    MatchSymbol("||", TokenType.Or) ||
                    MatchKeyword("for", TokenType.For) ||
                    MatchKeyword("rule", TokenType.Rule) ||
                    MatchKeyword("true", TokenType.True) ||
                    MatchKeyword("false", TokenType.False) ||
                    MatchKeyword("if", TokenType.If) ||
                    MatchKeyword("break", TokenType.Break) ||
                    MatchKeyword("continue", TokenType.Continue) ||
                    MatchKeyword("define", TokenType.Define) ||
                    MatchNumber() ||
                    MatchIdentifier() ||
                    MatchString();
                
                if (!matched)
                    Unknown();
            }
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
            PushToken(new Token(Content[Index].ToString(), Index, 1, new DocRange(new DocPos(Line, Column), new DocPos(Line, Column + 1)), TokenType.Unknown));
            scanner.Advance();
            Accept(scanner);
        }

        /// <summary>Skips whitespace.</summary>
        public bool Skip()
        {
            bool preceedingWhitespace = false;
            LexScanner scanner = MakeScanner();
            while (scanner.AtWhitespace() && !scanner.ReachedEnd)
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
            _push.Add(token);
        }

        private void PushToken(LexScanner scanner, TokenType tokenType)
        {
            _push.Add(scanner.AsToken(tokenType));
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
        public bool ReachedEnd => Index == _content.Length;
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
            bool res = _content[Index] == character;
            Advance();
            return res;
        }

        public bool At(char chr) => _content[Index] == chr;
        public bool AtIdentifierChar() => identifierCharacters.Contains(_content[Index]);
        public bool AtWhitespace() => whitespaceCharacters.Contains(_content[Index]);
        public bool AtNumeric() => numericalCharacters.Contains(_content[Index]);
        public Token AsToken(TokenType tokenType) => new Token(_captured.ToString(), Index, Index + _captured.Length, new DocRange(_startPos, new DocPos(Line, Column)), tokenType);
    }
}