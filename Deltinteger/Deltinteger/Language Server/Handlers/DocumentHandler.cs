namespace Deltin.Deltinteger.LanguageServer;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using Settings;
using Model;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using MediatR;
using System.Linq;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspPositition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

public class DocumentHandler : ITextDocumentSyncHandler
{
    // Static
    private static readonly TextDocumentSyncKind _syncKind = TextDocumentSyncKind.Incremental;
    private static readonly bool _sendTextOnSave = true;

    // Object
    readonly List<Document> _documents = new();
    readonly OstwLangServer _languageServer;
    readonly ParserSettingsResolver _parserSettingsResolver;
    readonly ILangLogger _logger;

    public DocumentHandler(LanguageServerBuilder builder)
    {
        _languageServer = builder.Server;
        _parserSettingsResolver = builder.ParserSettingsResolver;
        _logger = builder.LangLogger;
    }

    public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new TextDocumentAttributes(uri, "ostw");

    // Document change
    public TextDocumentChangeRegistrationOptions GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentChangeRegistrationOptions()
    {
        DocumentSelector = OstwLangServer.DocumentSelector,
        SyncKind = _syncKind
    };

    // Open
    TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentOpenRegistrationOptions()
    {
        DocumentSelector = OstwLangServer.DocumentSelector
    };

    // Close
    TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentCloseRegistrationOptions()
    {
        DocumentSelector = OstwLangServer.DocumentSelector
    };

    // Save
    TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSaveRegistrationOptions()
    {
        DocumentSelector = OstwLangServer.DocumentSelector
    };

    // Handle save.
    public Task<Unit> Handle(DidSaveTextDocumentParams saveParams, CancellationToken token)
    {
        var uri = saveParams.TextDocument.Uri.ToUri();
        _logger.LogMessage("Saving " + uri);

        var document = TextDocumentFromUri(uri);
        if (document is null)
        {
            _logger.LogMessage($"Attempted to save nonexistant document '{uri}'");
            return Unit.Task;
        }

        if (_sendTextOnSave)
        {
            document.UpdateIfChanged(saveParams.Text, _parserSettingsResolver.GetParserSettings(document.Uri));
        }
        _languageServer.ProjectUpdater.UpdateProject(document);
        return Unit.Task;
    }

    // Handle close.
    public Task<Unit> Handle(DidCloseTextDocumentParams closeParams, CancellationToken token)
    {
        var uri = closeParams.TextDocument.Uri.ToUri();
        _logger.LogMessage($"Closing '{uri}'");

        var removing = TextDocumentFromUri(uri);
        if (removing is null)
        {
            _logger.LogMessage($"Attempted to close nonexistant document '{uri}'");
            return Unit.Task;
        }

        removing.Remove();
        _documents.Remove(removing);
        return Unit.Task;
    }

    // Handle open.
    public Task<Unit> Handle(DidOpenTextDocumentParams openParams, CancellationToken token)
    {
        _logger.LogMessage($"Opening '{openParams.TextDocument.Uri}'");

        var newDocument = new Document(openParams.TextDocument);
        _documents.Add(newDocument);
        _languageServer.ProjectUpdater.UpdateProject(newDocument);
        return Unit.Task;
    }

    // Handle change.
    public Task<Unit> Handle(DidChangeTextDocumentParams changeParams, CancellationToken token)
    {
        var uri = changeParams.TextDocument.Uri.ToUri();
        var document = TextDocumentFromUri(uri);

        if (document is null)
        {
            _logger.LogMessage($"Attempted to update nonexistant document '{uri}' (open documents: {string.Join(", ", _documents.Select(d => $"'{d.Uri}'"))})");
            return Unit.Task;
        }

        foreach (var change in changeParams.ContentChanges)
        {
            int start = Extras.TextIndexFromPosition(document.Content, change.Range.Start);
            int length = Extras.TextIndexFromPosition(document.Content, change.Range.End) - start;

            StringBuilder rep = new StringBuilder(document.Content);
            rep.Remove(start, length);
            rep.Insert(start, change.Text);

            document.Update(rep.ToString(), change, changeParams.TextDocument.Version, _parserSettingsResolver.GetParserSettings(document.Uri));
        }

        _languageServer.ProjectUpdater.UpdateProject(document);
        return Unit.Task;
    }

    // ~ Public methods
    public IReadOnlyList<Document> GetDocuments() => _documents;

    public Document TextDocumentFromUri(Uri uri)
    {
        for (int i = 0; i < _documents.Count; i++)
            // TODO-URI: Should use Uri.Compare? 
            if (_documents[i].Uri == uri)
                return _documents[i];
        return null;
    }

    public Task AddDocumentAsync(Uri uri, string initialContent)
    {
        _logger.LogMessage($"Opening '{uri}'");

        var doc = new Document(uri, initialContent);
        _documents.Add(doc);
        _languageServer.ProjectUpdater.UpdateProject(doc);
        return Unit.Task;
    }

    public async Task ChangeDocumentAsync(Uri uri, InterpChangeEvent[] changes)
    {
        await Handle(new DidChangeTextDocumentParams()
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier()
            {
                Uri = uri,
                Version = null
            },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(changes.Select(c => new TextDocumentContentChangeEvent()
            {
                Range = c.range,
                RangeLength = c.rangeLength,
                Text = c.text
            }))
        }, CancellationToken.None);
    }
    // ~ End Public methods
}