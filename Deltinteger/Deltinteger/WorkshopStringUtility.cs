namespace Deltin;
using System;
using System.Text;
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
    public static StringChunk[][] ChunkSplit(string value, string splitHead, string splitTail, char[] stringCharacters)
    {
        // Escape the input string. Reconsider this if the value may be provided with something already escaped.
        value = value.Replace("\"", "\\\"");
        // Ensure parameters are not null.
        splitHead = splitHead ?? string.Empty;
        splitTail = splitTail ?? string.Empty;
        stringCharacters = stringCharacters ?? new char[0];

        var total = new List<List<StringChunk>>(); // 511 byte chunks
        var stubs = new List<StringChunk>(); // 128 byte stubs
        var currentStub = string.Empty;
        string lastValidDecoratedStub = null;
        var currentFormats = new List<int>();

        int position = 0;
        while (position < value.Length)
        {
            // four 128 byte strings can fit into 511 bytes, with 1 subtracted from the final stub.
            var isFirstStub = stubs.Count == 0;
            var isLastStub = stubs.Count == 3;

            // Get the next minimum chunk.
            var (addText, newPosition) = GetMinimumChunk(value, stringCharacters, currentFormats, position);
            currentStub += addText;
            position = newPosition;

            // The workshop uses UTF8 encoding.
            var currentStubWithDecorations = DecorateStub(currentStub, splitHead, splitTail, isFirstStub, isLastStub);
            var currentStubLength = Encoding.UTF8.GetByteCount(currentStubWithDecorations);

            // In theory this should be MAX_STRING_STUB_BYTE_LENGTH or MAX_STRING_STUB_BYTE_LENGTH - 1 if isLastStub,
            // but this seems to work fine.
            var max = Constants.MAX_STRING_STUB_BYTE_LENGTH + 1;

            if (currentStubLength >= max || currentFormats.Count > 3)
            {
                // If 'previousDecoratedStub' is null, splitHead + splitTail exceeds the workshop's string byte size.
                if (lastValidDecoratedStub == null)
                    throw new System.Exception("The lengths of the splitHead and splitTail parameters combined exceed " + Constants.MAX_STRING_STUB_BYTE_LENGTH + " bytes");

                // Add last valid stub.
                AddStub(stubs, lastValidDecoratedStub, currentFormats);

                // Update the current stub.
                currentStub = addText;
                lastValidDecoratedStub = DecorateStub(currentStub, splitHead, splitTail, isFirstStub, isLastStub);

                if (isLastStub)
                {
                    total.Add(stubs);
                    stubs = new();
                }
            }
            lastValidDecoratedStub = currentStubWithDecorations;
        }

        // Add remnant stub
        AddStub(stubs, lastValidDecoratedStub, currentFormats);
        total.Add(stubs);

        // List<List<StringChunk>> to StringChunk[][]
        return total.Select(t => t.ToArray()).ToArray();
    }

    static string DecorateStub(string stub, string splitHead, string splitTail, bool isFirstStub, bool isLastStub)
    {
        return isFirstStub ? splitTail + stub : stub + (isLastStub ? splitHead : "");
    }

    /// <summary>Adds a stub to a list of stubs.</summary>
    /// <param name="stubs">The list containing the stubs.</param>
    /// <param name="lastValidDecoratedStub">The stub text.</param>
    /// <param name="currentFormats">The stub's text formats.</param>
    static void AddStub(List<StringChunk> stubs, string lastValidDecoratedStub, List<int> currentFormats)
    {
        stubs.Add(new(lastValidDecoratedStub, currentFormats.Take(3).ToArray()));
        currentFormats.RemoveRange(0, Math.Min(3, currentFormats.Count));
    }

    /// <summary>
    /// When ChunkSplit is splitting text, the text shouldn't be split inside some elements.
    /// For example, element identifiers, strings, and formats would have issues if they were cut in the middle.
    /// GetMinimumCharacters will get the next set of characters at the provided position which shouldn't be separated.
    /// </summary>
    /// <param name="str">The input text.</param>
    /// <param name="stringCharacters">The characters which start and end strings such as ' and ".</param>
    /// <param name="currentFormats">The formats in the current stub. GetMinimumChunk may modify this.</param>
    /// <param name="position">The current text position.</param>
    static TextProgress GetMinimumChunk(
        string str,
        char[] stringCharacters,
        List<int> currentFormats,
        int position)
    {

        // Check for format.
        var formatChunk = GetFormatChunk(str, currentFormats, position);
        if (formatChunk.HasValue)
            return formatChunk.Value;

        // Check for string.
        var stringChunk = GetStringChunk(str, stringCharacters, position);
        if (stringChunk.HasValue)
            return stringChunk.Value;

        // Capture all text until next whitespace.
        string captured = string.Empty;
        do
        {
            captured += str[position];
            position++;
        }
        while (
            // Ensure the position does not go out of range.
            position < str.Length &&
            // Do not consume any opening curly brackets in case it its the start of a format.
            str[position] != '{' &&
            // This is only possible on the first 'do' iteration. If we get a whitespace, only return that whitespace.
            !char.IsWhiteSpace(captured.Last()) &&
            // The next character is a whitespace, we can stop here.
            !char.IsWhiteSpace(str[position]));

        return new(captured, position);
    }

    /// <summary>Checks for a format at the provided text position.</summary>
    static Nullable<TextProgress> GetFormatChunk(string str, List<int> currentFormats, int position)
    {
        // Format start
        // 'str.Length - 1' would also technically work, this would catch {} at eof though.
        if (str[position] == '{' && position < str.Length - 2)
        {
            position++;
            string number = string.Empty;
            // Get format number
            while (position < str.Length && char.IsNumber(str[position]))
            {
                number += str[position];
                position++;
            }
            // If number.Length == 0, then the text at position is just "{}" and is not a format.
            // The text at the end of the number must be '}'.
            if (number.Length > 0 && position < str.Length && str[position] == '}')
            {
                // The number in the format.
                int formatInput = int.Parse(number);
                int formatValue = currentFormats.IndexOf(formatInput);
                if (formatValue == -1)
                {
                    currentFormats.Add(formatInput);
                    formatValue = currentFormats.Count == 4 ? 0 : currentFormats.Count - 1;
                }
                return new("{" + formatValue + "}", position + 1);
            }
        }
        return null;
    }

    /// <summary>Checks for a string at the provided text position.</summary>
    static Nullable<TextProgress> GetStringChunk(string str, char[] stringCharacters, int position)
    {
        // Do nothing if not start of string.
        if (!stringCharacters.Contains(str[position]))
            return null;
        // Start chunk using string terminator.
        string chunk = str[position].ToString();
        // Save the character used to start the string.
        char terminateCharacter = str[position];
        bool escaping = false;
        // Progress past starting string character.
        position++;
        for (; position < str.Length; position++)
        {
            chunk += str[position];
            if (!escaping)
            {
                if (str[position] == '\\')
                    escaping = true;
                else if (str[position] == terminateCharacter)
                    break;
            }
            escaping = false;
        }
        return new(chunk, position + 1);
    }

    readonly record struct TextProgress(string Text, int NewPosition);

    /// <summary>Makes an OSTW string more compatible with the workshop.
    /// Escaped single quotes are unescaped, unescaped double quotes are escaped.</summary>
    /// <param name="raw">The OSTW string. First and last character should be ' or ".</param>
    public static string WorkshopStringFromRawText(string raw)
    {
        // Single or double quoted string?
        bool isSingle = raw.StartsWith('\'');
        // Trim starting and ending quotations.
        string withoutQuotes = raw.Substring(1, raw.Length - 2);

        // No special processing needs to happen with double quotes.
        if (!isSingle)
            return withoutQuotes;

        // Single quoted strings will be escaped, they do not need to be.
        withoutQuotes = withoutQuotes.Replace("\\'", "'");
        // Conversely, double quotes may not be escaped though they should be.
        return EscapedDoubleQuotes(withoutQuotes);
    }

    static string EscapedDoubleQuotes(string input)
    {
        string result = string.Empty;
        bool escaping = false;
        for (int i = 0; i < input.Length; i++)
        {
            if (escaping)
                escaping = false;
            else if (input[i] == '\\')
                escaping = true;
            else if (input[i] == '"')
                result += '\\';
            result += input[i];
        }
        return result;
    }
}

readonly record struct StringChunk(string Value, int[] Parameters);