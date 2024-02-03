#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Compiler.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Cache;
using TextDocumentItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextDocumentItem;
using Deltin.Deltinteger.Compiler.Parse.Lexing;

namespace Deltin.Deltinteger.Compiler;

public class Document
{
    public Uri Uri { get; }
    public string Content { get; private set; }
    public int? Version { get; private set; }
    public ParserSettings ParserSettings { get; private set; }
    public CacheWatcher Cache { get; } = new CacheWatcher();
    public DocumentParseResult? ParseResult { get; private set; }

    public Document(Uri uri, string initialContent)
    {
        Uri = uri;
        Content = initialContent;
        Parse();
    }

    public Document(TextDocumentItem document) : this(document.Uri.ToUri(), document.Text)
    {
        Version = document.Version;
    }

    private void Parse()
    {
        try
        {
            var lexer = new Lexer(ParserSettings.Default);
            lexer.Init(new(Content));
            var parser = new Parser(lexer, ParserSettings, ParseResult?.Syntax);
            var syntax = parser.Parse();

            var completedTokenList = lexer.CurrentController.GetCompletedTokenList();
            ParseResult = new(syntax, completedTokenList, completedTokenList.GetErrors().Concat(parser.Errors));
        }
        catch (Exception ex)
        {
            ErrorReport.Add(ex.ToString());
            ParseResult = new(new(), new(new()), Array.Empty<IParserError>());
        }
    }

    public void Update(string newContent, UpdateRange updateRange, int? version, ParserSettings parserSettings)
    {
        Version = version;
        Content = newContent;

        if (!parserSettings.Equals(ParserSettings))
        {
            ParserSettings = parserSettings;
            Parse();
        }
        else
        {
            Parse();
            // incremental lexer is having some issues
            // todo: make toggleable in ParserSettings!
            // Lexer.Update(new VersionInstance(newContent), updateRange);
            // Parse();
        }
    }

    public void UpdateIfChanged(string newContent, ParserSettings parserSettings)
    {
        if (newContent == null || (newContent == Content && parserSettings.Equals(ParserSettings))) return;
        Update(newContent, parserSettings);
    }

    public void Update(string newContent, ParserSettings parserSettings)
    {
        Content = newContent;
        ParserSettings = parserSettings;
        Parse();
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
        Text = Content,
        LanguageId = "ostw"
    };

    public void Remove()
    {
        Cache.Unregister();
    }
}

public record DocumentParseResult(
    RootContext Syntax,
    ReadonlyTokenList Tokens,
    IEnumerable<IParserError> ParserErrors);