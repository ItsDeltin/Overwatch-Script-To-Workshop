#nullable enable
using Deltin.Deltinteger.Compiler.File;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public readonly record struct LexerIncrementalChange(
    TokenList Tokens,
    int ChangeStartToken,
    int StopLexingAtIndex,
    int InitialTokenCount)
{
    public static LexerIncrementalChange Update(
        TokenList tokens,
        VersionInstance previousContent,
        VersionInstance newContent,
        DocumentUpdateRange updateRange)
    {
        AffectedAreaInfo affectedArea = GetAffectedArea(tokens, updateRange, previousContent);

        int indexOffset = updateRange.Text.Length - (previousContent.IndexOf(updateRange.Range.End) - previousContent.IndexOf(updateRange.Range.Start));
        int finalTokenInChange = affectedArea.StartingTokenIndex + affectedArea.Length;
        int initialTokenCount = tokens.Count;

        // Adjust token ranges.
        for (int i = finalTokenInChange; i < tokens.Count; i++)
        {
            if (updateRange.Range.End <= tokens[i].Range.Start)
            {
                // Use the old content for getting the update range index.
                int s = previousContent.IndexOf(tokens[i].Range.Start) + indexOffset,
                    e = previousContent.IndexOf(tokens[i].Range.End) + indexOffset;

                // Use the new content to update the positions.
                newContent.UpdatePosition(tokens[i].Range.Start, s);
                newContent.UpdatePosition(tokens[i].Range.End, e);

                var node = tokens.GetNode(i);
                node.StartPosition = newContent.GetLexPosition(node.StartPosition.Index + indexOffset);
                node.EndPosition = newContent.GetLexPosition(node.EndPosition.Index + indexOffset);
            }
        }
        int endIndex = tokens.Count == 0 || finalTokenInChange >= tokens.Count ? newContent.Length :
            newContent.IndexOf(tokens[finalTokenInChange].Range.Start);

        // Remove old tokens
        for (int i = finalTokenInChange - 1; i >= affectedArea.StartingTokenIndex; i--)
        {
            tokens.RemoveAt(i);
        }

        return new(
            tokens,
            affectedArea.StartingTokenIndex,
            endIndex,
            initialTokenCount
        );
    }

    static (int, int) GetChangeDelta(UpdateRange updateRange)
    {
        var vi = new VersionInstance(updateRange.Text);
        int lineDelta = vi.NumberOfLines() - updateRange.Range.LineSpan();
        int columnDelta = updateRange.Text.Length - vi.IndexOfLastLine() - updateRange.Range.ColumnSpan();
        return (lineDelta, columnDelta);
    }

    static AffectedAreaInfo GetAffectedArea(TokenList tokens, DocumentUpdateRange updateRange, VersionInstance previousContent)
    {
        // Get the range of the tokens overlapped.
        bool startSet = false;
        int startingTokenIndex = 0; // The position in the token list where new tokens will be inserted into.
        int length = 0;
        int lineOfChangeStart = updateRange.Range.Start.Line;
        int lineOfChangeEnd = updateRange.Range.End.Line;

        // Find the first token to the left of the update range and the first token to the right of the update range.
        // Set 'startIndex', 'startingTokenIndex' with the left token and 'endingTokenIndex' with the right token.
        // 
        // In the event of a left-side or right-side token in relation to the update range is not found,
        // the default values of 'startIndex', 'startingTokenIndex', and 'endingTokenIndex' should handle it fine.
        for (int i = 0; i < tokens.Count; i++)
        {
            // If the current token overlaps the update range or intersect with a line of the change, set the startTokenIndex.
            if (tokens[i].Range.DoOverlap(updateRange.Range) || (tokens[i].Range.End.Line >= lineOfChangeStart && tokens[i].Range.Start.Line <= lineOfChangeEnd))
            {
                // Don't set the starting position and index again if it was already set via overlap.
                // We use a seperate if rather than an && because the proceeding else-ifs cannot run if the token overlaps the update range.
                if (!startSet)
                {
                    // Set the starting token to the current token's index.
                    startingTokenIndex = i;
                    length = 0;

                    // If the previous token touches the current starting token index, then shift the starting token back.
                    // This is only required due to the ... spread token.
                    // When the user writes '..', that is seen as [dot, dot]. Once the final dot is added, the change range is set to
                    // the middle dot, so the first dot isn't lexed and we get [dot, dot, dot] instead of [spread].
                    // If a token such as '..' is added (range operator), then this can be removed.
                    while (startingTokenIndex > 0 &&
                        tokens[startingTokenIndex].Range.Start.EqualTo(
                            tokens[startingTokenIndex - 1].Range.End
                        ))
                    {
                        startingTokenIndex--;
                        length++;
                    }

                    // Once an overlapping token is found, do not set 'startIndex' or 'startingTokenIndex' again.
                    startSet = true;
                }
                length++;
            }
            // Sometimes, there is no overlapping token. In that case, we use the closest token to the left of the update range.
            // We determine if a token is to the left by checking if the token's ending position is less than the update range's start position.
            else if (!startSet && tokens[i].Range.End < updateRange.Range.Start)
            {
                // Set the starting token to the current token's index.
                startingTokenIndex = i;
                length = 1;
            }
            // The i'th token is past the update range.
            else break;
        }

        // Shift the starting token to the first in the current line.
        // This is required due to workshop symbols having spaces between the,.
        while (startingTokenIndex > 0 &&
            tokens[startingTokenIndex - 1].Range.End.Line == tokens[startingTokenIndex].Range.Start.Line)
        {
            startingTokenIndex--;
            length++;
        }

        // Stretch length to end of line
        while (startingTokenIndex + length < tokens.Count &&
            tokens[startingTokenIndex].Range.End.Line ==
            tokens[startingTokenIndex + length].Range.Start.Line)
            length++;

        return new AffectedAreaInfo(startingTokenIndex, length);
    }

    /// <summary>The affected range of a file when it's contents were changed.</summary>
    /// <param name="StartingTokenIndex">The index of the exising token stream where the change starts.</param>
    readonly record struct AffectedAreaInfo(int StartingTokenIndex, int Length);
}
