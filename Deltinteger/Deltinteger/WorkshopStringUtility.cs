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
    /// <returns>A 2d string where the first dimension is the 511 byte segments and the second dimension is the
    /// 128 byte chunks.</returns>
    public static string[][] ChunkSplit(string value, string splitHead, string splitTail)
    {
        // Escape the input string. Reconsider this if the value may be provided with something already escaped.
        value = value.Replace("\"", "\\\"");
        // Ensure parameters are not null.
        splitHead = splitHead ?? string.Empty;
        splitTail = splitTail ?? string.Empty;

        var total = new List<List<string>>(); // 511 byte chunks
        var stubs = new List<string>(); // 128 byte stubs
        var currentStub = string.Empty;
        var validStub = string.Empty;
        string lastValidDecoratedStub = null;

        for (int i = 0; i < value.Length; i++)
        {
            // four 128 byte strings can fit into 511 bytes, with 1 subtracted from the final stub.
            var isFirstStub = stubs.Count == 0;
            var isLastStub = stubs.Count == 3;

            currentStub += value[i];
            validStub += value[i];
            var currentStubWithDecorations = DecorateStub(currentStub, splitHead, splitTail, isFirstStub, isLastStub);
            // The workshop uses UTF8 encoding.
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
            else if (char.IsWhiteSpace(value[i]) || i == value.Length - 1)
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