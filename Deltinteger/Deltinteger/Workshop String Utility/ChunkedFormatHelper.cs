namespace Deltin.WorkshopString;
using System;
using System.Linq;
using System.Collections.Generic;

struct ChunkedFormatHelper
{
    List<int> currentFormats;
    const int MAX_FORMATS = 2;

    public ChunkedFormatHelper()
    {
        currentFormats = new List<int>();
    }

    public int GetFormat(int userInput)
    {
        int workshopValue = currentFormats.IndexOf(userInput);
        if (workshopValue == -1)
        {
            currentFormats.Add(userInput);
            workshopValue = currentFormats.Count - 1;
        }
        return workshopValue % MAX_FORMATS;
    }

    public bool OverCapacity() => currentFormats.Count > MAX_FORMATS;

    public IEnumerable<StringChunkParameter> ExtractStub()
    {
        var stubFormats = currentFormats.Take(MAX_FORMATS).ToArray();
        currentFormats.RemoveRange(0, Math.Min(MAX_FORMATS, currentFormats.Count));
        return stubFormats.Select(format => new StringChunkParameter.InputParameter(format));
    }

    public string GetAppendText()
    {
        return "{" + currentFormats.Take(MAX_FORMATS).Count() + "}";
    }
}