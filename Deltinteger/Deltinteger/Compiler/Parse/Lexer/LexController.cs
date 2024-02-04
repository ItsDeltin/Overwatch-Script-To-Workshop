#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class LexController
{
    // Inputs
    readonly ParserSettings parserSettings;
    readonly string content;
    readonly VanillaSymbols vanillaSymbols;

    // State
    readonly TokenList listed = new();
    int lockedInTokens = 0;

    public LexController(ParserSettings parserSettings, string content, VanillaSymbols vanillaSymbols)
    {
        this.parserSettings = parserSettings;
        this.content = content;
        this.vanillaSymbols = vanillaSymbols;
    }

    public void ProgressTo(int tokenIndex)
    {
        lockedInTokens = tokenIndex;
    }

    public Token? GetTokenAt(int token, LexerContextKind contextKind)
    {
        // Token is already known but the context was changed.
        if (token < listed.Count && contextKind != listed.GetNode(token).ContextKind)
        {
            DiscardCurrentState();
        }

        // Scan tokens until requested position.
        while (listed.Count <= token)
        {
            var scanAt = GetScanPositionForToken(token);
            var nextTokens = MatchTokensAtPosition(scanAt, contextKind);

            // Add tokens
            if (nextTokens is not null)
            {
                foreach (var add in nextTokens)
                    listed.Add(add.MatchedToken, add.NewPosition, contextKind, add.Error);
            }
            else return null;
        }


        return listed[token];
    }

    public Token GetTokenAtOrLast(int token, LexerContextKind contextKind)
    {
        return GetTokenAt(token, contextKind) ?? listed.Last();
    }

    void DiscardCurrentState()
    {
        for (int i = lockedInTokens; i < listed.Count; i++)
            listed.RemoveAt(i);
    }

    LexPosition GetScanPositionForToken(int token)
    {
        if (token == 0)
        {
            return LexPosition.Zero;
        }
        else
        {
            return listed.GetNode(token - 1).EndPosition;
        }
    }

    private Match[]? MatchTokensAtPosition(LexPosition position, LexerContextKind contextKind)
    {
        var matcher = new LexMatcher(parserSettings, content, vanillaSymbols, contextKind, position);
        return matcher.MatchOne();
    }

    public ReadonlyTokenList GetCompletedTokenList() => new(listed);
}

public class LexMatcher
{
    private readonly ParserSettings _settings;
    private readonly string _content;
    private readonly VanillaSymbols _vanillaSymbols;
    private readonly LexerContextKind _contextKind;
    private LexPosition _position;

    public LexMatcher(
        ParserSettings settings,
        string content,
        VanillaSymbols vanillaSymbols,
        LexerContextKind contextKind,
        LexPosition position)
    {
        _settings = settings;
        _content = content;
        _vanillaSymbols = vanillaSymbols;
        _contextKind = contextKind;
        _position = position;
    }

    public Match[]? MatchOne()
    {
        _position = Skip(null);

        if (_position.Index >= _content.Length)
            return null;

        Match[]? matched = _contextKind switch
        {
            LexerContextKind.Workshop => MatchWorkshopContext(),
            LexerContextKind.LobbySettings => MatchLobbySettingsContext(),
            LexerContextKind.Normal or _ => One(MatchDefault())
        };

        if (matched is not null)
            return matched;
        else
            return One(Unknown());
    }

    Match? MatchDefault() => Match(
        MatchActionComment,
        MatchNumber,
        () => MatchSymbol("..", TokenType.Spread),
        MatchCSymbol,
        () => MatchSymbol('~', TokenType.Squiggle),
        () => MatchSymbol("|", TokenType.Pipe),
        () => MatchSymbol('@', TokenType.At),
        () => MatchKeyword("import", TokenType.Import),
        () => MatchKeyword("for", TokenType.For),
        () => MatchKeyword("while", TokenType.While),
        () => MatchKeyword("foreach", TokenType.Foreach),
        () => MatchKeyword("in", TokenType.In),
        () => MatchKeyword("rule", TokenType.Rule),
        () => MatchKeyword("disabled", TokenType.Disabled),
        () => MatchKeyword("true", TokenType.True),
        () => MatchKeyword("false", TokenType.False),
        () => MatchKeyword("null", TokenType.Null),
        () => MatchKeyword("if", TokenType.If),
        () => MatchKeyword("else", TokenType.Else),
        () => MatchKeyword("break", TokenType.Break),
        () => MatchKeyword("continue", TokenType.Continue),
        () => MatchKeyword("return", TokenType.Return),
        () => MatchKeyword("switch", TokenType.Switch),
        () => MatchKeyword("case", TokenType.Case),
        () => MatchKeyword("default", TokenType.Default),
        () => MatchKeyword("class", TokenType.Class),
        () => MatchKeyword("struct", TokenType.Struct),
        () => MatchKeyword("enum", TokenType.Enum),
        () => MatchKeyword("new", TokenType.New),
        () => MatchKeyword("delete", TokenType.Delete),
        () => MatchKeyword("define", TokenType.Define),
        () => MatchKeyword("void", TokenType.Void),
        () => MatchKeyword("public", TokenType.Public),
        () => MatchKeyword("private", TokenType.Private),
        () => MatchKeyword("protected", TokenType.Protected),
        () => MatchKeyword("static", TokenType.Static),
        () => MatchKeyword("override", TokenType.Override),
        () => MatchKeyword("virtual", TokenType.Virtual),
        () => MatchKeyword("recursive", TokenType.Recursive),
        () => MatchKeyword("globalvar", TokenType.GlobalVar),
        () => MatchKeyword("playervar", TokenType.PlayerVar),
        () => MatchKeyword("persist", TokenType.Persist),
        () => MatchKeyword("ref", TokenType.Ref),
        () => MatchKeyword("this", TokenType.This),
        () => MatchKeyword("root", TokenType.Root),
        () => MatchKeyword("async", TokenType.Async),
        () => MatchKeyword("constructor", TokenType.Constructor),
        () => MatchKeyword("as", TokenType.As),
        () => MatchKeyword("type", TokenType.Type),
        () => MatchKeyword("single", TokenType.Single),
        () => MatchKeyword("const", TokenType.Const),
        () => MatchKeyword("json", TokenType.Json),
        () => MatchKeyword("variables", TokenType.WorkshopVariablesEn),
        () => MatchKeyword("subroutines", TokenType.WorkshopSubroutinesEn),
        () => MatchKeyword("settings", TokenType.WorkshopSettingsEn),
        () => MatchVanillaKeyword(_vanillaSymbols.Variables, TokenType.WorkshopVariables),
        () => MatchVanillaKeyword(_vanillaSymbols.Variables, TokenType.WorkshopSubroutines),
        () => MatchVanillaKeyword(_vanillaSymbols.Variables, TokenType.WorkshopSettings),
        MatchIdentifier,
        () => MatchString());

    Match[]? MatchWorkshopContext() =>
        MatchMany(
            () => One(MatchNumber()),
            () => One(MatchCSymbol()),
            () => MatchVanillaConstantWithDisabledMoniker(_vanillaSymbols.ScriptSymbols),
            () => One(MatchString()),
            () => One(MatchVanillaKeyword(_vanillaSymbols.AllTeams, TokenType.AllTeams)),
            () => One(MatchVanillaKeyword(_vanillaSymbols.Team1, TokenType.Team1)),
            () => One(MatchVanillaKeyword(_vanillaSymbols.Team2, TokenType.Team2)),
            () => One(MatchVanillaConstant(_vanillaSymbols.ScriptSymbols)),
            () => One(MatchVanillaKeyword(_vanillaSymbols.Actions, TokenType.WorkshopActions)),
            () => One(MatchVanillaKeyword(_vanillaSymbols.Conditions, TokenType.WorkshopConditions)),
            () => One(MatchVanillaKeyword(_vanillaSymbols.Event, TokenType.WorkshopEvent)),
            () => One(MatchVanillaSymbol()));

    Match[]? MatchLobbySettingsContext()
    {
        var disabledSettingMatch = MatchVanillaConstantWithDisabledMoniker(_vanillaSymbols.LobbySettings);
        if (disabledSettingMatch is not null)
            return disabledSettingMatch;

        var match = Match(
            () => MatchVanillaConstant(_vanillaSymbols.LobbySettings),
            MatchNumber,
            () => MatchString(),
            MatchCSymbol,
            MatchVanillaSymbol
        );
        return One(match);
    }

    Match? MatchCSymbol() => Match(
        () => MatchSymbol('{', TokenType.CurlyBracket_Open),
        () => MatchSymbol('}', TokenType.CurlyBracket_Close),
        () => MatchSymbol('(', TokenType.Parentheses_Open),
        () => MatchSymbol(')', TokenType.Parentheses_Close),
        () => MatchSymbol('[', TokenType.SquareBracket_Open),
        () => MatchSymbol(']', TokenType.SquareBracket_Close),
        () => MatchSymbol(':', TokenType.Colon),
        () => MatchSymbol('?', TokenType.QuestionMark),
        () => MatchSymbol(';', TokenType.Semicolon),
        () => MatchSymbol('.', TokenType.Dot),
        () => MatchSymbol("=>", TokenType.Arrow),
        () => MatchSymbol("!=", TokenType.NotEqual),
        () => MatchSymbol("==", TokenType.EqualEqual),
        () => MatchSymbol("<=", TokenType.LessThanOrEqual),
        () => MatchSymbol(">=", TokenType.GreaterThanOrEqual),
        () => MatchSymbol('!', TokenType.Exclamation),
        () => MatchSymbol("^=", TokenType.HatEqual),
        () => MatchSymbol("*=", TokenType.MultiplyEqual),
        () => MatchSymbol("/=", TokenType.DivideEqual),
        () => MatchSymbol("%=", TokenType.ModuloEqual),
        () => MatchSymbol("+=", TokenType.AddEqual),
        () => MatchSymbol("-=", TokenType.SubtractEqual),
        () => MatchSymbol('=', TokenType.Equal),
        () => MatchSymbol('<', TokenType.LessThan),
        () => MatchSymbol('>', TokenType.GreaterThan),
        () => MatchSymbol(',', TokenType.Comma),
        () => MatchSymbol('^', TokenType.Hat),
        () => MatchSymbol('*', TokenType.Multiply),
        () => MatchSymbol('/', TokenType.Divide),
        () => MatchSymbol('%', TokenType.Modulo),
        () => MatchSymbol("++", TokenType.PlusPlus),
        () => MatchSymbol('+', TokenType.Add),
        () => MatchSymbol("--", TokenType.MinusMinus),
        () => MatchSymbol('-', TokenType.Subtract),
        () => MatchSymbol("&&", TokenType.And),
        () => MatchSymbol("||", TokenType.Or));

    static Match[]? One(Match? match) => match is null ? null : new[] { match.Value };

    static Match? Match(params Func<Match?>[] matchers)
    {
        foreach (var match in matchers)
        {
            var m = match();
            if (m is not null)
                return m;
        }
        return null;
    }

    static Match[]? MatchMany(params Func<Match[]?>[] matchers)
    {
        foreach (var match in matchers)
        {
            var m = match();
            if (m is not null)
                return m;
        }
        return null;
    }

    private LexScanner MakeScanner(LexPosition? position = default) => new(position ?? _position, _content);
    private WhitespaceLexScanner MakeWsLexScanner(LexPosition? position = default) => new(position ?? _position, _content);

    // * Matchers *

    /// <summary>Matches a keyword.</summary>
    /// <param name="keyword">The name of the keyword that will be matched.</param>
    /// <param name="tokenType">The type of the created token.</param>
    /// <returns>Whether the keyword was matched.</returns>
    Match? MatchKeyword(string keyword, TokenType tokenType)
    {
        LexScanner scanner = MakeScanner();
        if (scanner.Match(keyword) && !scanner.AtIdentifierChar())
        {
            return GetMatch(scanner, tokenType);
        }
        return null;
    }

    /// <summary>Matches a symbol.</summary>
    /// <returns>Whether a symbol was matched.</returns>
    Match? MatchSymbol(string symbol, TokenType tokenType)
    {
        LexScanner scanner = MakeScanner();
        if (scanner.Match(symbol))
        {
            return GetMatch(scanner, tokenType);
        }
        return null;
    }

    /// <summary>Matches a symbol.</summary>
    /// <returns>Whether a symbol was matched.</returns>
    Match? MatchSymbol(char symbol, TokenType tokenType)
    {
        LexScanner scanner = MakeScanner();
        if (scanner.Match(symbol))
        {
            return GetMatch(scanner, tokenType);
        }
        return null;
    }

    /// <summary>Matches an identifier.</summary>
    /// <returns>Whether an identifier was matched.</returns>
    Match? MatchIdentifier()
    {
        LexScanner scanner = MakeScanner();

        // Advance while the current character is an identifier.
        while (!scanner.ReachedEnd && scanner.AtIdentifierChar()) scanner.Advance();

        // Push the token if it is accepted.
        return scanner.WasAdvanced ? GetMatch(scanner, TokenType.Identifier) : null;
    }

    /// <summary>Matches a string. Works with single or double quotes and escaping.</summary>
    /// <returns>Whether a string was matched.</returns>
    Match? MatchString(bool continueInterpolatedString = false, bool single = false)
    {
        LexScanner scanner = MakeScanner();

        // Interpolated string.
        bool interpolated = continueInterpolatedString || scanner.Match('$');

        if (!continueInterpolatedString)
        {
            // single will be true for single quotes, false for double quotes.
            single = scanner.Match('\'');

            // Not a string.
            if (!single && !scanner.Match('\"')) return null;
        }

        char lookingFor = single ? '\'' : '\"';

        //escaped will be 0 whenever it's not escaped
        bool escaped = false;
        // Look for end of string.
        while (!scanner.ReachedEnd && (escaped || !scanner.Match(lookingFor)))
        {
            var progressCheck = scanner.Position.Index;

            // If this is an interpolated string, look for a '{' that is not followed by another '{'.
            if (interpolated && scanner.Match('{') && !scanner.Match('{'))
            {
                var result = GetMatch(
                    scanner,
                    continueInterpolatedString ? TokenType.InterpolatedStringMiddle : TokenType.InterpolatedStringTail,
                    single ? TokenFlags.StringSingleQuotes : TokenFlags.None);

                return result;
            }

            escaped = !escaped && scanner.Match('\\');

            // If the scanner did not progress, advance.
            if (progressCheck == scanner.Position.Index)
                scanner.Advance();
        }

        return GetMatch(scanner, interpolated ? TokenType.InterpolatedStringHead : TokenType.String);
    }

    /// <summary>Matches a number.</summary>
    /// <returns>Whether a number was matched.</returns>
    Match? MatchNumber()
    {
        LexScanner scanner = MakeScanner();

        // Get the number.
        bool foundLeftNumber = false;
        while (scanner.AtNumeric())
        {
            scanner.Advance();
            foundLeftNumber = true;
        }

        Skip(scanner);

        // At decimal
        if (scanner.At('.'))
        {
            scanner.Advance();
            Skip(scanner);

            // Get the decimal.
            bool decimalFound = false;
            while (scanner.AtNumeric())
            {
                scanner.Advance();
                decimalFound = true;
            }

            if (!decimalFound && !foundLeftNumber)
                return null;
        }
        // No decimal and no left number.
        else if (!foundLeftNumber)
            return null;

        // Done.
        return GetMatch(scanner, TokenType.Number);
    }

    Match? MatchActionComment()
    {
        LexScanner scanner = MakeScanner();
        // Action comment.
        if (!scanner.At('#')) return null;

        // Match every character to the end of the line.
        scanner.Advance();
        while (!scanner.ReachedEnd && !scanner.At('\n')) scanner.Advance();

        // Done.
        return GetMatch(scanner, TokenType.ActionComment);
    }

    Match? MatchLineComment(LexScanner? scanner = null)
    {
        scanner ??= MakeScanner();
        if (!scanner.At('/') || !scanner.At('/', 1)) return null;

        scanner.Advance();
        scanner.Advance();

        // Match every character to the end of the line.
        while (!scanner.ReachedEnd && !scanner.At('\n')) scanner.Advance();

        // Done.
        return GetMatch(scanner, TokenType.LineComment);
    }

    Match? MatchBlockComment(LexScanner? scanner = null)
    {
        scanner ??= MakeScanner();
        if (!scanner.At('/') || !scanner.At('*', 1)) return null;

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

        return GetMatch(scanner, TokenType.BlockComment);
    }

    Match[]? MatchVanillaConstantWithDisabledMoniker(WorkshopSymbolTrie symbolSet)
    {
        // Try to match the entire constant at the current position.
        // ie: "[disabled heroes]"
        var tryWholeWord = MatchVanillaConstant(symbolSet);

        // Match the 'disabled' keyword.
        // ie: "[disabled] heroes"
        var disabledKw = MatchVanillaKeyword(VanillaInfo.LowercaseDisabled, TokenType.DisabledWorkshopItem);
        if (disabledKw is null) return One(tryWholeWord);

        // Try to match a workshop setting after the disabled keyword.
        // ie: "disabled [heroes]"
        var disabling = MatchVanillaConstant(symbolSet, NextWhitespace(disabledKw.Value.NewPosition));
        if (disabling is null) return One(tryWholeWord);

        // Choose between the ambiguous "[disabled heroes]" or "[disabled] [heroes]"
        if (tryWholeWord is not null && tryWholeWord.Value.NewPosition.Index >= disabling.Value.NewPosition.Index)
            return One(tryWholeWord);
        else
            return new[] {
                disabledKw.Value,
                disabling.Value
            };
    }

    Match? MatchVanillaConstant(WorkshopSymbolTrie symbolSet, LexPosition? at = default)
    {
        var scanner = MakeWsLexScanner(at);

        var symbolTraveller = symbolSet.Travel();
        // Feed incoming characters into the symbol traveller
        while (scanner.Next(out char current) && symbolTraveller.Next(char.ToLower(current)))
            scanner.Advance();

        // Do not create a workshop symbol in the middle of a word,
        // ex: "[Small Message]s()"
        bool isAtEndOfTerm = !scanner.Next(out char value) ||
            CharData.WhitespaceCharacters.Contains(value) ||
            VanillaInfo.StructureCharacters.Contains(value);

        var word = symbolTraveller.Word();
        if (isAtEndOfTerm && word.HasValue)
            return GetMatch(scanner, TokenType.WorkshopConstant, word.Value.LinkedItems);

        return null;
    }

    Match? MatchVanillaSymbol()
    {
        var scanner = MakeWsLexScanner();

        while (scanner.Next(out char current) &&
            !VanillaInfo.StructureCharacters.Contains(current))
            scanner.Advance();

        if (scanner.DidAdvance())
            return GetMatch(scanner, TokenType.WorkshopSymbol);

        return null;
    }

    Match? MatchVanillaKeyword(VanillaKeyword keyword, TokenType tokenType) => MatchSymbol(keyword.EnUs, tokenType);

    Match? MatchVanillaDouble()
    {
        var scanner = MakeWsLexScanner();

        MatchError? error = null;

        // Negative number
        bool isNegative = scanner.Next() is '-';
        if (isNegative)
            scanner.Advance();

        // First number
        bool gotIntegerPart = false;
        while (scanner.Next(out char next) && CharData.NumericalCharacters.Contains(next))
        {
            gotIntegerPart = true;
            scanner.Advance();
        }

        // Not a number
        if (!isNegative && !gotIntegerPart)
        {
            return null;
        }
        // Float
        else if (scanner.Next() is '.')
        {
            scanner.Advance(); // consume '.'
            bool gotFractionalPart = false;
            while (scanner.Next(out char next) && CharData.NumericalCharacters.Contains(next))
            {
                gotFractionalPart = true;
                scanner.Advance();
            }

            if (!gotFractionalPart)
            {
                error = new("Number is missing decimal");
            }
        }
        // - with no number.
        else if (isNegative && !gotIntegerPart)
        {
            error = new("Number is missing integer");
        }

        return GetMatch(scanner, TokenType.Number, error);
    }

    /// <summary>The current character is unknown.</summary>
    Match Unknown()
    {
        LexScanner scanner = MakeScanner();
        scanner.Advance();
        return GetMatch(scanner, TokenType.Unknown);
    }

    /// <summary>Skips whitespace.</summary>
    LexPosition Skip(LexScanner? scanner = default)
    {
        scanner ??= MakeScanner();

        do
        {
            while (scanner.AtWhitespace())
                scanner.Advance();
        } while (MatchLineComment(scanner).Or(MatchBlockComment(scanner)).HasValue);

        return scanner.Position;
    }

    LexPosition NextWhitespace(LexPosition from) => Skip(MakeScanner(from));

    static Match GetMatch(LexScanner scanner, TokenType tokenType) => new(scanner.AsToken(tokenType), scanner.Position);
    static Match GetMatch(LexScanner scanner, TokenType tokenType, TokenFlags flags)
    {
        var token = scanner.AsToken(tokenType);
        token.Flags = flags;
        return new(token, scanner.Position);
    }
    static Match GetMatch(WhitespaceLexScanner scanner, TokenType tokenType) => new(scanner.AsToken(tokenType), scanner.CurrentPosition());
    static Match GetMatch(WhitespaceLexScanner scanner, TokenType tokenType, MatchError? error) => new(scanner.AsToken(tokenType), scanner.CurrentPosition(), error);
    static Match GetMatch(WhitespaceLexScanner scanner, TokenType tokenType, IReadOnlySet<LanguageLinkedWorkshopItem> workshopItems) => new(
        scanner.AsToken(tokenType, workshopItems),
        scanner.CurrentPosition());
}

public record struct Match(Token MatchedToken, LexPosition NewPosition, MatchError? Error = null)
{
    public void JumpTo(LexPosition position)
    {
        NewPosition = position;
    }
}