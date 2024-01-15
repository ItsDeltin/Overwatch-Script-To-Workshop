using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class Lexer
    {
        public List<Token> Tokens { get; } = new List<Token>();
        public VersionInstance Content { get; private set; }
        public LexController CurrentController { get; private set; }
        public int IncrementalChangeStart { get; private set; }
        public int IncrementalChangeEnd { get; private set; }
        public bool IsPushCompleted { get; private set; }
        public int TokenCount => Tokens.Count;
        private ITokenPush _currentTokenPush;
        private int _lastTokenCount;
        private readonly ParserSettings _parseSettings;
        private readonly Stack<LexerContextKind> _context = new(new[] { LexerContextKind.Normal });

        public Lexer(ParserSettings parseSettings)
        {
            _parseSettings = parseSettings;
        }

        public void Init(VersionInstance content)
        {
            Content = content;
            CurrentController = new LexController(_parseSettings, Content.Text, _currentTokenPush = new InitTokenPush(this), VanillaSymbols.Instance);
        }

        public void Reset()
        {
            Tokens.Clear();
            Content = null;
            IsPushCompleted = false;
        }

        public void Update(VersionInstance newContent, UpdateRange updateRange)
        {
            IsPushCompleted = false;
            _lastTokenCount = Tokens.Count;
            AffectedAreaInfo affectedArea = GetAffectedArea(updateRange);

            // The number of lines
            int lineDelta = NumberOfNewLines(updateRange.Text) - updateRange.Range.LineSpan();
            int columnDelta = NumberOfCharactersInLastLine(updateRange.Text) - updateRange.Range.ColumnSpan();

            int indexOffset = updateRange.Text.Length - (Content.IndexOf(updateRange.Range.End) - Content.IndexOf(updateRange.Range.Start));

            // Adjust token ranges.
            for (int i = affectedArea.EndingTokenIndex; i < Tokens.Count; i++)
            {
                if (updateRange.Range.End <= Tokens[i].Range.Start)
                {
                    // Use the old content for getting the update range index.
                    int s = Content.IndexOf(Tokens[i].Range.Start) + indexOffset,
                        e = Content.IndexOf(Tokens[i].Range.End) + indexOffset;

                    // Use the new content to update the positions.
                    newContent.UpdatePosition(Tokens[i].Range.Start, s);
                    newContent.UpdatePosition(Tokens[i].Range.End, e);
                }
            }

            _currentTokenPush = new IncrementalTokenInsert(this, affectedArea.StartingTokenIndex, affectedArea.EndingTokenIndex);
            CurrentController = new LexController(_parseSettings, newContent.Text, _currentTokenPush, VanillaSymbols.Instance);

            // Set start range
            CurrentController.Index = affectedArea.StartIndex;
            CurrentController.Line = newContent.GetLine(CurrentController.Index);
            CurrentController.Column = newContent.GetColumn(CurrentController.Index);

            Content = newContent;
            IncrementalChangeStart = affectedArea.StartingTokenIndex;
            IncrementalChangeEnd = affectedArea.EndingTokenIndex;
        }

        AffectedAreaInfo GetAffectedArea(UpdateRange updateRange)
        {
            // Get the range of the tokens overlapped.
            bool startSet = false;

            // The default starting range of the update range's start and end positions in the document.
            int updateStartIndex = Content.IndexOf(updateRange.Range.Start);

            int startIndex = updateStartIndex; // The position where lexing will start.
            int startingTokenIndex = -1; // The position in the token list where new tokens will be inserted into.
            int endingTokenIndex = int.MaxValue; // The token where lexing will end.

            // If there are no tokens or the update range preceeds the range of the first token, set the starting index to 0.
            if (Tokens.Count == 0 || updateRange.Range.End < Tokens[0].Range.Start) startIndex = 0;

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
                        // Set the starting token to the current token's index.
                        startingTokenIndex = i;

                        // If the previous token touches the current starting token index, then shift the starting token back.
                        // This is only required due to the ... spread token.
                        // When the user writes '..', that is seen as [dot, dot]. Once the final dot is added, the change range is set to
                        // the middle dot, so the first dot isn't lexed and we get [dot, dot, dot] instead of [spread].
                        // If a token such as '..' is added (range operator), then this can be removed.
                        while (startingTokenIndex > 0 &&
                            Tokens[startingTokenIndex].Range.Start.EqualTo(
                                Tokens[startingTokenIndex - 1].Range.End
                            ))
                        {
                            startingTokenIndex--;
                        }

                        // 'startIndex' cannot be higher than 'updateStartIndex'.
                        // Simply setting 'startIndex' to 'IndexOf...' may cause an issue in the common scenario:

                        // Character            : |1|2|3|4|5|6|7|
                        // Update start position:    x     x
                        // Token start position :        +     +

                        // startIndex will be 3, causing the characters between the first x and + to be skipped.
                        // So pick the lower of the 2 values.
                        startIndex = Math.Min(updateStartIndex, Content.IndexOf(Tokens[startingTokenIndex].Range.Start));
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
                    startIndex = Math.Min(updateStartIndex, Content.IndexOf(Tokens[i].Range.Start));
                    // Set the starting token to the current token's index.
                    startingTokenIndex = i;
                }
                // If the token was overlapping, it would have been caught earlier.
                // This block will run once no more overlapping tokens are found.
                else if (startSet)
                {
                    // Subtract by 1 because i - 1 is the last token that overlaps with the update range.
                    endingTokenIndex = i;

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

        public Token ScanTokenAt(int tokenIndex) => ScanTokenAt(tokenIndex, CurrentController.MatchOne);

        public Token ScanTokenAt(int tokenIndex, Action<LexerContextKind> match)
        {
            while (!IsPushCompleted && !_currentTokenPush.IncrementalStop() && _currentTokenPush.Current <= tokenIndex)
                match(_context.Peek());
            return Tokens.ElementAtOrDefault(tokenIndex);
        }

        public bool IsFinished(int currentToken) => currentToken >= TokenCount && IsPushCompleted;

        public void PushCompleted()
        {
            IsPushCompleted = true;
            IncrementalChangeEnd = Math.Max(IncrementalChangeEnd, _currentTokenPush.Current);
        }

        public int GetTokenDelta()
        {
            if (!IsPushCompleted)
                throw new Exception("Cannot get token delta until the current token push is completed.");
            return Tokens.Count - _lastTokenCount;
        }

        public T InVanillaWorkshopContext<T>(Func<T> task)
        {
            _context.Push(LexerContextKind.Workshop);
            var result = task();
            _context.Pop();
            return result;
        }

        public T InSettingsContext<T>(Func<T> task)
        {
            _context.Push(LexerContextKind.LobbySettings);
            var result = task();
            _context.Pop();
            return result;
        }

        private static int NumberOfNewLines(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    count++;
            return count;
        }

        private static int NumberOfCharactersInLastLine(string text) => text.Split('\n').Last().Length;
    }

    public enum LexerContextKind
    {
        Normal,
        Workshop,
        LobbySettings
    }

    public class LexController
    {
        public int Index;
        public int Line;
        public int Column;
        public string Content { get; }
        private readonly ITokenPush _push;
        private readonly VanillaSymbols _vanillaSymbols;
        private readonly ParserSettings _settings;

        public LexController(ParserSettings settings, string content, ITokenPush push, VanillaSymbols vanillaSymbols)
        {
            Content = content;
            _push = push;
            _vanillaSymbols = vanillaSymbols;
            _settings = settings;
        }

        public void MatchOne(LexerContextKind contextKind)
        {
            Skip();
            if (HasMoreContent())
            {
                bool didMatch = contextKind switch
                {
                    LexerContextKind.Workshop => MatchWorkshopContext(),
                    LexerContextKind.LobbySettings => MatchLobbySettingsContext(),
                    LexerContextKind.Normal or _ => MatchDefault()
                };

                if (didMatch)
                    Skip();
                else
                    Unknown();
            }
            PostMatch();
        }

        public bool MatchDefault()
        {
            return
                MatchActionComment() ||
                MatchNumber() ||
                MatchSymbol("..", TokenType.Spread) ||
                MatchSymbol('~', TokenType.Squiggle) ||
                MatchSymbol("|", TokenType.Pipe) ||
                MatchSymbol('@', TokenType.At) ||
                MatchCSymbol() ||
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
                MatchKeyword("return", TokenType.Return) ||
                MatchKeyword("switch", TokenType.Switch) ||
                MatchKeyword("case", TokenType.Case) ||
                MatchKeyword("default", TokenType.Default) ||
                MatchKeyword("class", TokenType.Class) ||
                MatchKeyword("struct", TokenType.Struct) ||
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
                MatchKeyword("persist", TokenType.Persist) ||
                MatchKeyword("ref", TokenType.Ref) ||
                MatchKeyword("this", TokenType.This) ||
                MatchKeyword("root", TokenType.Root) ||
                MatchKeyword("async", TokenType.Async) ||
                MatchKeyword("constructor", TokenType.Constructor) ||
                MatchKeyword("as", TokenType.As) ||
                MatchKeyword("type", TokenType.Type) ||
                MatchKeyword("single", TokenType.Single) ||
                MatchKeyword("const", TokenType.Const) ||
                MatchKeyword("json", TokenType.Json) ||
                MatchKeyword("variables", TokenType.WorkshopVariablesEn) ||
                MatchKeyword("subroutines", TokenType.WorkshopSubroutinesEn) ||
                MatchKeyword("settings", TokenType.WorkshopSettingsEn) ||
                MatchVanillaKeyword(_vanillaSymbols.Variables, TokenType.WorkshopVariables) ||
                MatchVanillaKeyword(_vanillaSymbols.Variables, TokenType.WorkshopSubroutines) ||
                MatchVanillaKeyword(_vanillaSymbols.Variables, TokenType.WorkshopSettings) ||
                MatchIdentifier() ||
                MatchString();
        }

        public bool MatchWorkshopContext()
        {
            return
                MatchNumber() ||
                MatchCSymbol() ||
                MatchString() ||
                MatchVanillaConstant(_vanillaSymbols.ScriptSymbols) ||
                MatchVanillaKeyword(_vanillaSymbols.Actions, TokenType.WorkshopActions) ||
                MatchVanillaKeyword(_vanillaSymbols.Conditions, TokenType.WorkshopConditions) ||
                MatchVanillaKeyword(_vanillaSymbols.Event, TokenType.WorkshopEvent) ||
                MatchVanillaSymbol();
        }

        public bool MatchLobbySettingsContext()
        {
            return
                MatchVanillaConstant(_vanillaSymbols.LobbySettings) ||
                MatchNumber() ||
                MatchCSymbol() ||
                MatchSymbol('%', TokenType.PercentSign) ||
                MatchVanillaSymbol();
        }

        public bool MatchCSymbol()
        {
            return MatchSymbol('{', TokenType.CurlyBracket_Open) ||
                MatchSymbol('}', TokenType.CurlyBracket_Close) ||
                MatchSymbol('(', TokenType.Parentheses_Open) ||
                MatchSymbol(')', TokenType.Parentheses_Close) ||
                MatchSymbol('[', TokenType.SquareBracket_Open) ||
                MatchSymbol(']', TokenType.SquareBracket_Close) ||
                MatchSymbol(':', TokenType.Colon) ||
                MatchSymbol('?', TokenType.QuestionMark) ||
                MatchSymbol(';', TokenType.Semicolon) ||
                MatchSymbol('.', TokenType.Dot) ||
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
                MatchSymbol("||", TokenType.Or);
        }

        bool HasMoreContent() => Index < Content.Length && !_push.IncrementalStop();

        public void PostMatch()
        {
            if (Index >= Content.Length) _push.EndReached();
        }

        private LexScanner MakeScanner() => new LexScanner(this);

        private void Accept(LexScanner scanner)
        {
            Index = scanner.Index;
            Line = scanner.Line;
            Column = scanner.Column;
        }

        private void Accept(WhitespaceLexScanner whitespaceLexScanner)
        {
            (Index, Line, Column) = whitespaceLexScanner.CurrentPosition();
        }

        // * Matchers *

        /// <summary>Matches a keyword.</summary>
        /// <param name="keyword">The name of the keyword that will be matched.</param>
        /// <param name="tokenType">The type of the created token.</param>
        /// <returns>Whether the keyword was matched.</returns>
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
        /// <returns>Whether a symbol was matched.</returns>
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
        /// <returns>Whether a symbol was matched.</returns>
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
        /// <returns>Whether an identifier was matched.</returns>
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
        /// <returns>Whether a string was matched.</returns>
        public bool MatchString(bool continueInterpolatedString = false, bool single = false)
        {
            LexScanner scanner = MakeScanner();

            // Interpolated string.
            bool interpolated = continueInterpolatedString || scanner.Match('$');

            if (!continueInterpolatedString)
            {
                // single will be true for single quotes, false for double quotes.
                single = scanner.Match('\'');

                // Not a string.
                if (!single && !scanner.Match('\"')) return false;
            }

            char lookingFor = single ? '\'' : '\"';

            //escaped will be 0 whenever it's not escaped
            bool escaped = false;
            // Look for end of string.
            while (!scanner.ReachedEnd && (escaped || !scanner.Match(lookingFor)))
            {
                var progressCheck = scanner.Index;

                // If this is an interpolated string, look for a '{' that is not followed by another '{'.
                if (interpolated && scanner.Match('{') && !scanner.Match('{'))
                {
                    Token resultingToken = scanner.AsToken(continueInterpolatedString ? TokenType.InterpolatedStringMiddle : TokenType.InterpolatedStringTail);
                    if (single) resultingToken.Flags |= TokenFlags.StringSingleQuotes;
                    PushToken(resultingToken);
                    Accept(scanner);
                    return true;
                }

                escaped = escaped ? false : scanner.Match('\\');

                // If the scanner did not progress, advance.
                if (progressCheck == scanner.Index)
                    scanner.Advance();
            }

            PushToken(scanner, interpolated ? TokenType.InterpolatedStringHead : TokenType.String);
            return true;
        }

        /// <summary>Matches a number.</summary>
        /// <returns>Whether a number was matched.</returns>
        public bool MatchNumber()
        {
            LexScanner scanner = MakeScanner();

            // Get the number.
            bool foundLeftNumber = false;
            while (scanner.AtNumeric())
            {
                scanner.Advance();
                foundLeftNumber = true;
            }

            Skip();

            // At decimal
            if (scanner.At('.'))
            {
                scanner.Advance();
                Skip();

                // Get the decimal.
                bool decimalFound = false;
                while (scanner.AtNumeric())
                {
                    scanner.Advance();
                    decimalFound = true;
                }

                if (!decimalFound && !foundLeftNumber)
                    return false;
            }
            // No decimal and no left number.
            else if (!foundLeftNumber)
                return false;

            // Done.
            PushToken(scanner, TokenType.Number);
            return true;
        }

        bool MatchActionComment()
        {
            LexScanner scanner = MakeScanner();
            // Action comment.
            if (!scanner.At('#')) return false;

            // Match every character to the end of the line.
            scanner.Advance();
            while (!scanner.ReachedEnd && !scanner.At('\n')) scanner.Advance();

            // Done.
            PushToken(scanner, TokenType.ActionComment);
            return true;
        }

        bool MatchLineComment()
        {
            LexScanner scanner = MakeScanner();
            if (!scanner.At('/') || !scanner.At('/', 1)) return false;

            scanner.Advance();
            scanner.Advance();

            // Match every character to the end of the line.
            while (!scanner.ReachedEnd && !scanner.At('\n')) scanner.Advance();

            // Done.
            Accept(scanner);
            return true;
        }

        bool MatchBlockComment()
        {
            LexScanner scanner = MakeScanner();
            if (!scanner.At('/') || !scanner.At('*', 1)) return false;

            scanner.Advance();
            scanner.Advance();

            // Match every character to the end of the line.
            while (!scanner.ReachedEnd)
            {
                if (scanner.At('*') && scanner.At('/', 1))
                {
                    scanner.Advance();
                    scanner.Advance();
                    break;
                }
                scanner.Advance();
            }

            // Done.
            Accept(scanner);
            return true;
        }

        bool MatchVanillaConstant(WorkshopSymbolTrie symbolSet)
        {
            var scanner = new WhitespaceLexScanner(this);

            var symbolTraveller = symbolSet.Travel();
            // Feed incoming characters into the symbol traveller
            while (scanner.Next(out char current) && symbolTraveller.Next(current))
                scanner.Advance();

            // Do not create a workshop symbol in the middle of a word,
            // ex: "[Small Message]s()"
            bool isAtEndOfTerm = !scanner.Next(out char value) ||
                LexScanner.whitespaceCharacters.Contains(value) ||
                VanillaInfo.StructureCharacters.Contains(value);

            var word = symbolTraveller.Word();
            if (isAtEndOfTerm && word.HasValue)
            {
                PushToken(scanner.AsToken(TokenType.WorkshopConstant, word.Value.LinkedItems));
                Accept(scanner);
                return true;
            }

            return false;
        }

        bool MatchVanillaSymbol()
        {
            var scanner = new WhitespaceLexScanner(this);

            while (scanner.Next(out char current) &&
                !VanillaInfo.StructureCharacters.Contains(current))
                scanner.Advance();

            if (scanner.DidAdvance())
            {
                PushToken(scanner.AsToken(TokenType.WorkshopSymbol));
                Accept(scanner);
                return true;
            }

            return false;
        }

        bool MatchVanillaKeyword(VanillaKeyword keyword, TokenType tokenType) => MatchSymbol(keyword.EnUs, tokenType);

        /// <summary>The current character is unknown.</summary>
        public void Unknown()
        {
            LexScanner scanner = MakeScanner();
            PushToken(new Token(Content[Index].ToString(), new DocRange(new DocPos(Line, Column), new DocPos(Line, Column + 1)), TokenType.Unknown));
            scanner.Advance();
            Accept(scanner);
        }

        /// <summary>Skips whitespace.</summary>
        public void Skip()
        {
            do
            {
                LexScanner scanner = MakeScanner();
                while (!scanner.ReachedEnd && scanner.AtWhitespace())
                    scanner.Advance();
                Accept(scanner);
            } while (MatchLineComment() || MatchBlockComment());
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
        public static readonly char[] whitespaceCharacters = " \t\r\n".ToCharArray();
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
            if (!ReachedEnd && _content[Index] == character)
            {
                Advance();
                return true;
            }
            return false;
        }

        public char? Current() => _content.ElementAtOrDefault(Index);
        public bool At(char chr) => !ReachedEnd && _content[Index] == chr;
        public bool At(char chr, int lookahead) => Index + lookahead < _content.Length && _content[Index + lookahead] == chr;
        public bool AtIdentifierChar() => !ReachedEnd && identifierCharacters.Contains(_content[Index]);
        public bool AtWhitespace() => !ReachedEnd && whitespaceCharacters.Contains(_content[Index]);
        public bool AtNumeric() => !ReachedEnd && numericalCharacters.Contains(_content[Index]);
        public Token AsToken(TokenType tokenType) => new Token(_captured.ToString(), new DocRange(_startPos, new DocPos(Line, Column)), tokenType);
    }

    struct WhitespaceLexScanner
    {
        public record struct Position(int Index, int Line, int Column);

        Position _start;
        Position _currentPosition;
        Position _lastNonWhitespacePosition;
        readonly string _content;

        public WhitespaceLexScanner(LexController controller)
        {
            _currentPosition = new(controller.Index, controller.Line, controller.Column);
            _lastNonWhitespacePosition = _currentPosition;
            _start = _currentPosition;
            _content = controller.Content;
        }

        public readonly bool Next(out char value)
        {
            value = _content.ElementAtOrDefault(_currentPosition.Index);
            return !ReachedEnd();
        }

        public readonly bool ReachedEnd() => _currentPosition.Index >= _content.Length;

        public readonly Position CurrentPosition() => _lastNonWhitespacePosition;

        public void Advance()
        {
            if (ReachedEnd()) return;

            char current = _content[_currentPosition.Index];
            if (current == '\n')
            {
                _currentPosition.Line++;
                _currentPosition.Column = 0;
            }
            else _currentPosition.Column++;
            _currentPosition.Index++;

            if (!LexScanner.whitespaceCharacters.Contains(current))
            {
                _lastNonWhitespacePosition = _currentPosition;
            }
        }

        public readonly bool DidAdvance() => _currentPosition != _start;

        private readonly string Text() => _content.Substring(_start.Index, _lastNonWhitespacePosition.Index - _start.Index);

        private readonly DocRange Range() => new(new(_start.Line, _start.Column), new(_lastNonWhitespacePosition.Line, _lastNonWhitespacePosition.Column));

        public readonly WorkshopToken AsToken(TokenType tokenType, IReadOnlySet<LanguageLinkedWorkshopItem> workshopItems)
        {
            return new(Text(), Range(), tokenType, workshopItems);
        }

        public readonly Token AsToken(TokenType tokenType)
        {
            return new(Text(), Range(), tokenType);
        }
    }

    /// <summary>
    /// When the LexController scans a token, implementers will handle the obtained token.
    /// There are just 2 implementers: the InitTokenPush and the IncrementalTokenInsert.
    /// </summary>
    public interface ITokenPush
    {
        int Current { get; }
        void PushToken(Token token);
        bool IncrementalStop();
        void EndReached();
    }

    /// <summary>The first lex of a file will use this for the ITokenPush.
    /// When a token is encountered, simply add the token to the token list.</summary>
    class InitTokenPush : ITokenPush
    {
        public int Current => _tokens.Count;
        private readonly Lexer _lexer;
        private readonly List<Token> _tokens;
        private bool _finished = false;

        public InitTokenPush(Lexer lexer)
        {
            _lexer = lexer;
            _tokens = lexer.Tokens;
        }

        public void PushToken(Token token) => _tokens.Add(token);
        public bool IncrementalStop() => _finished;
        public void EndReached()
        {
            _finished = true;
            _lexer.PushCompleted();
        }
    }

    /// <summary>Every subsequent lex after the first will use this for the ITokenPush.
    /// This is used for updating the lexer incrementally. When a token is encountered, it will be
    /// inserted into the token list depending on where the update occured.</summary>
    class IncrementalTokenInsert : ITokenPush
    {
        public int Current { get; private set; } // Where in the token list the tokens will be inserted to when a token is found.
        private readonly Lexer _lexer;
        private readonly List<Token> _tokens; // The list of tokens.
        private bool _lexUntilEnd = true; // Determines if lexing should occur until the end of the file is reached.
        private int _stopAt; // The index to stop lexing at.
        private bool _lastInsertWasEqual; // Will be true when we have resynced with the tokens.

        public IncrementalTokenInsert(Lexer lexer, int startingTokenIndex, int stopMinimum)
        {
            Current = startingTokenIndex;
            _lexer = lexer;
            _tokens = lexer.Tokens;
            if (stopMinimum < _tokens.Count)
            {
                _stopAt = stopMinimum;
                _lexUntilEnd = false;
            }
        }

        public void PushToken(Token token)
        {
            if (_lastInsertWasEqual) return;
            // This is used to determine whether _stopAt is correctly being incremented as tokens are inserted.
            // System.Diagnostics.Debug.Assert(_lexUntilEnd || _tokens.IndexOf(_debugEndToken) == _stopAt);

            // Check if we have reached the end of the changed token range.
            // If *all* of these conditions are met, '_lastInsertWasEqual' will be set to true, the block will be skipped and lexing will stop.
            if (!(_lastInsertWasEqual =
                // First, check if _lexUntilEnd is false. _lexUntilEnd may be true when adding characters to the end of the file.
                // If it is true, we don't need to worry about stopping early since we are lexing until the end of the file.
                !_lexUntilEnd &&
                // Check if the 'Current' index is equal to or greater than _stopAt.
                Current >= _stopAt &&
                // Make sure Current is less than the count of the _tokens list.
                // This can occur if a token near the end of the file was changed that is not the last token.
                Current < _tokens.Count &&
                // Now we make sure the token we got is equal to the Current token. If it is, we don't need to lex anymore.
                // Make sure the type, range, and text are equal. In most scenarios if TokenType is the same then text will be the same, the only exceptions being numbers, strings, and identifiers.
                _tokens[Current].TokenType == token.TokenType &&
                _tokens[Current].Range.Equals(token.Range) &&
                _tokens[Current].Text == token.Text
            ))
            {
                // Specific case where the starting token was not found, insert the current token to the first index.
                if (Current == -1)
                {
                    _tokens.Insert(0, token);
                    // Set Current to 1.
                    // This increment will set Current from -1 to 0,
                    // the next increment will set Current from 0 to 1.
                    Current++;
                    _stopAt++; // The ending token was pushed up.
                }
                // If Current surpassed the token count, add the token to the top of the list.
                else if (Current == _tokens.Count)
                {
                    _tokens.Add(token);
                    _stopAt++; // The ending token was pushed up.
                }
                // If Current is the at or pass the index of the ending token, insert the token to the current position.
                else if (!_lexUntilEnd && Current >= _stopAt)
                {
                    _tokens.Insert(Current, token);
                    _stopAt++; // The ending token was pushed up.
                }
                // If none of the previous conditions were satisfied,
                // replace the current token.
                else
                {
                    _tokens.RemoveAt(Current);
                    _tokens.Insert(Current, token);
                    // The number of tokens is unchanged, so we don't need to change _stopAt.
                }
            }
            Current++;
        }

        public bool IncrementalStop() => _lastInsertWasEqual;

        public void EndReached()
        {
            if (Current != -1)
                while (_tokens.Count > Current)
                    _tokens.RemoveAt(Current);
            _lexer.PushCompleted();
        }
    }

    /// <summary>The affected range of a file when it's contents were changed.</summary>
    class AffectedAreaInfo
    {
        /// <summary>The starting character where the change occured.</summary>
        public int StartIndex { get; }
        /// <summary>The index of the exising token stream where the change starts.</summary>
        public int StartingTokenIndex { get; }
        /// <summary>The index of the exising token stream where the change ends.</summary>
        public int EndingTokenIndex { get; }

        public AffectedAreaInfo(int startIndex, int startingTokenIndex, int endingTokenIndex)
        {
            StartIndex = startIndex;
            StartingTokenIndex = startingTokenIndex;
            EndingTokenIndex = endingTokenIndex;
        }
    }

}

