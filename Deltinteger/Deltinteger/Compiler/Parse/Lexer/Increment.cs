#nullable enable
#if false
namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

static class IncrementLexer
{
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

        CurrentController = new LexController(_parseSettings, newContent.Text, VanillaSymbols.Instance, Tokens);

        // Set start range
        // CurrentController.Index = affectedArea.StartIndex;
        // CurrentController.Line = newContent.GetLine(CurrentController.Index);
        // CurrentController.Column = newContent.GetColumn(CurrentController.Index);

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
}
#endif