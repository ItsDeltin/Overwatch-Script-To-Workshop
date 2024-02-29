#nullable enable
using System;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Cache;
using TextDocumentItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextDocumentItem;
using Deltin.Deltinteger.Compiler.Parse.Lexing;
using System.Diagnostics;
using Deltin.Deltinteger.Compiler.File;

namespace Deltin.Deltinteger.Compiler;

public class Document
{
    public Uri Uri { get; }
    public int? Version { get; private set; }
    public ParserSettings ParserSettings { get; private set; } = ParserSettings.Default;
    public CacheWatcher Cache { get; } = new CacheWatcher();
    public DocumentParseResult? ParseResult { get; private set; }

    private VersionInstance content;

    public Document(Uri uri, string initialContent)
    {
        Uri = uri;
        content = new(initialContent);
        Parse(content);
    }

    public Document(TextDocumentItem document) : this(document.Uri.ToUri(), document.Text)
    {
        Version = document.Version;
    }

    private void Parse(VersionInstance newContent, IncrementalParse? incrementalParse = null)
    {
        content = newContent;
        try
        {
            var parser = new Parser(newContent, ParserSettings, incrementalParse);
            ParseResult = parser.Parse();
        }
        catch (Exception ex)
        {
            ErrorReport.Add(ex.ToString());
            ParseResult = DocumentParseResult.Default;
        }
    }

    public void Update(DocumentUpdateRange updateRange, ParserSettings parserSettings)
    {
        var newContent = new VersionInstance(updateRange.ApplyChangeToString(content.Text));

        if (!parserSettings.Equals(ParserSettings))
        {
            ParserSettings = parserSettings;
            Parse(newContent);
        }
        else if (ParseResult is not null)
        {
            var change = ParseResult.Update(newContent, updateRange);
            Parse(newContent, change);
        }
        else
        {
            Parse(newContent);
        }
    }

    public void UpdateIfChanged(string newContent, ParserSettings parserSettings)
    {
        if (newContent is null || (newContent == content.Text && parserSettings.Equals(ParserSettings))) return;
        Update(newContent, parserSettings);
    }

    public void Update(string newContent, ParserSettings parserSettings)
    {
        ParserSettings = parserSettings;
        Parse(new(newContent));
    }

    public Diagnostic[] GetDiagnostics()
    {
        if (ParseResult is not null)
        {
            return ParseResult.ParserErrors.Select(e => e.GetDiagnostic()).ToArray();
        }
        return Array.Empty<Diagnostic>();
    }

    public TextDocumentItem AsItem() => new TextDocumentItem()
    {
        Uri = Uri,
        Text = content.Text,
        LanguageId = "ostw"
    };

    public void Remove()
    {
        Cache.Unregister();
    }

    public string GetContent() => content.Text;
}
