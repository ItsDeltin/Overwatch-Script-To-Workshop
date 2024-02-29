#nullable enable
using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public enum LexerContextKind
{
    Normal,
    Workshop,
    LobbySettings,
    InterpolatedStringSingle,
    InterpolatedStringDouble
}

/// <summary>A position in the document.</summary>
/// <param name="Index">The character index of the position.</param>
/// <param name="Line">The line of the position.</param>
/// <param name="Column">The current column in the line.</param>
public record struct LexPosition(int Index, int Line, int Column)
{
    public static readonly LexPosition Zero = new();

    public static implicit operator DocPos(LexPosition lexPosition) => new(lexPosition.Line, lexPosition.Column);
}

/// <summary>An error that opccured while matching a token.</summary>
public record struct MatchError(string Message);

/// <summary>Contains data for parsing incrementally.</summary>
public readonly record struct IncrementalParse(LexerIncrementalChange IncrementalLexer, IReadOnlyList<TokenCapture> NodeCaptures)
{
    public readonly int ChangeStartToken() => IncrementalLexer.ChangeStartToken;
}