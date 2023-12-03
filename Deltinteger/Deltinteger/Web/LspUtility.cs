namespace Deltin.Deltinteger.Web;

using System;
using System.Linq;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;
using MarkedStringsOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkedStringsOrMarkupContent;

#nullable enable

static class LspUtility
{
    public static string[] GetMarkdownContent(MarkedStringsOrMarkupContent? markedStringsOrMarkupContent)
    {
        // Markup content 
        if (markedStringsOrMarkupContent?.MarkupContent?.Value is not null)
            return new string[] { markedStringsOrMarkupContent.MarkupContent.Value };
        // Marked string
        else if (markedStringsOrMarkupContent?.MarkedStrings is not null)
            return markedStringsOrMarkupContent.MarkedStrings.Select(s => s.Value ?? "").ToArray();
        // Nothing
        return Array.Empty<string>();
    }

    public static string GetMarkdownContent(StringOrMarkupContent? stringOrMarkupContent)
    {
        // string
        if (stringOrMarkupContent?.String is not null)
            return stringOrMarkupContent.String;
        // Markup content
        else if (stringOrMarkupContent?.MarkupContent?.Value is not null)
            return stringOrMarkupContent.MarkupContent.Value;
        // Nothing
        return "";
    }
}