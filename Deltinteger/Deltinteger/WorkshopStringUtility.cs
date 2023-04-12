namespace Deltin;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Constants = Deltin.Deltinteger.Constants;

static class WorkshopStringUtility
{
    /// <summary>Splits a string into 511 bytes long segments, with each of those segments split into 128 byte chunks.
    /// A string literal in the workshop can only be 128 bytes long, but by appending strings you can reach a
    /// maximum of 511 bytes.</summary>
    /// <param name="value">The input string that will be segmented and chunked.</param>
    /// <param name="splitHead">An optional value that is appended to the end of each 511 byte segments.</param>
    /// <param name="splitTail">An optional value that is prepended to the start of each 511 byte segments.</param>
    /// <param name="stringCharacters">An optional value that contains the characters used to start/end strings.
    /// Stubs will not be split mid string.</param>
    /// <returns>A 2d string where the first dimension is the 511 byte segments and the second dimension is the
    /// 128 byte chunks.</returns>
    public static string[][] ChunkSplit(string value, string splitHead, string splitTail, char[] stringCharacters)
    {
        // Escape the input string. Reconsider this if the value may be provided with something already escaped.
        value = value.Replace("\"", "\\\"");
        // Ensure parameters are not null.
        splitHead = splitHead ?? string.Empty;
        splitTail = splitTail ?? string.Empty;
        stringCharacters = stringCharacters ?? new char[0];

        var total = new List<List<string>>(); // 511 byte chunks
        var stubs = new List<string>(); // 128 byte stubs
        var currentStub = string.Empty;
        var validStub = string.Empty;
        string lastValidDecoratedStub = null;
        char? currentStringCharacter = null;

        for (int i = 0; i < value.Length; i++)
        {
            // four 128 byte strings can fit into 511 bytes, with 1 subtracted from the final stub.
            var isFirstStub = stubs.Count == 0;
            var isLastStub = stubs.Count == 3;

            currentStub += value[i];
            validStub += value[i];

            // Enter/exit string
            if (stringCharacters.Contains(value[i]))
            {
                // Not in string
                if (currentStringCharacter == null)
                    currentStringCharacter = value[i];
                // In string, if terminator matches the character that started the string then exit the string.
                else if (value[i] == currentStringCharacter)
                    currentStringCharacter = null;
            }

            // The workshop uses UTF8 encoding.
            var currentStubWithDecorations = DecorateStub(currentStub, splitHead, splitTail, isFirstStub, isLastStub);
            var currentStubLength = Encoding.UTF8.GetByteCount(currentStubWithDecorations);

            // In theory this should be MAX_STRING_STUB_BYTE_LENGTH or MAX_STRING_STUB_BYTE_LENGTH - 1 if isLastStub,
            // but this seems to work fine.
            var max = Constants.MAX_STRING_STUB_BYTE_LENGTH + 1;

            if (currentStubLength >= max)
            {
                // If 'previousDecoratedStub' is null, splitHead + splitTail exceeds the workshop's string byte size.
                if (lastValidDecoratedStub == null)
                    throw new System.Exception("The lengths of the splitHead and splitTail parameters combined exceed " + Constants.MAX_STRING_STUB_BYTE_LENGTH + " bytes");

                stubs.Add(lastValidDecoratedStub);
                currentStub = validStub;
                validStub = string.Empty;
                lastValidDecoratedStub = DecorateStub(currentStub, splitHead, splitTail, isFirstStub, isLastStub);

                if (isLastStub)
                {
                    total.Add(stubs);
                    stubs = new List<string>();
                }
            }
            // Only split stubs if the current character is whitespace and we are not currently in a string.
            // Or if this is the last character run the block so that the remaining content gets added.
            else if ((char.IsWhiteSpace(value[i]) && currentStringCharacter == null) || i == value.Length - 1)
            {
                lastValidDecoratedStub = currentStubWithDecorations;
                validStub = string.Empty;
            }
        }

        stubs.Add(lastValidDecoratedStub);
        total.Add(stubs);

        // List<List<string>> to string[][]
        return total.Select(t => t.ToArray()).ToArray();
    }

    static string DecorateStub(string stub, string splitHead, string splitTail, bool isFirstStub, bool isLastStub)
    {
        return isFirstStub ? splitTail + stub : stub + (isLastStub ? splitHead : "");
    }
}