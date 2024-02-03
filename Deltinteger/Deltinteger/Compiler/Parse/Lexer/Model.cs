#nullable enable
namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public enum LexerContextKind
{
    Normal,
    Workshop,
    LobbySettings
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

public record struct LexPosition(int Index, int Line, int Column)
{
    public static readonly LexPosition Zero = new();
}

