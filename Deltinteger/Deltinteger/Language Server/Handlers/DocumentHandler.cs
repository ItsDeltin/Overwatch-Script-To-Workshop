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
    private readonly IDocumentEvent _documentEvent;
    private readonly OstwLangServer _languageServer;
    private readonly ParserSettingsResolver _parserSettingsResolver;
    private readonly DsTomlWatcher _projectSettings;

    // Async script handling
    /// <summary>Active when compilation is complete.</summary>
    private TaskCompletionSource<Unit> _scriptReady = new();
    /// <summary>The active document.</summary>
    private Document _currentDocument;
    /// <summary>To prevent compiling on each keypress, wait a bit before
    /// compiling.</summary>
    private readonly ManualResetEventSlim _wait = new(false);
    private readonly ManualResetEventSlim _parseDone = new(false);
    private readonly CancellationTokenSource _stopUpdateListener = new();
    private Task _updateTask;

    public DocumentHandler(LanguageServerBuilder builder, IDocumentEvent documentEventHandler)
    {
        _documentEvent = documentEventHandler;
        _languageServer = builder.Server;
        _parserSettingsResolver = builder.ParserSettingsResolver;
        _projectSettings = builder.ProjectSettings;
        SetupUpdateListener();
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
        System.Diagnostics.Debug.WriteLine("documents", "Saving " + saveParams.TextDocument.Uri);
        if (_sendTextOnSave)
        {
            var document = TextDocumentFromUri(saveParams.TextDocument.Uri.ToUri());
            document.UpdateIfChanged(saveParams.Text, _parserSettingsResolver.GetParserSettings(document.Uri));
            return UpdateProjectAsync(document);
        }
        else return UpdateProjectAsync(saveParams.TextDocument.Uri.ToUri());
    }

    // Handle close.
    public Task<Unit> Handle(DidCloseTextDocumentParams closeParams, CancellationToken token)
    {
        System.Diagnostics.Debug.WriteLine("documents", "Closing " + closeParams.TextDocument.Uri);
        var removing = TextDocumentFromUri(closeParams.TextDocument.Uri.ToUri());
        removing.Remove();
        _documents.Remove(removing);
        return Unit.Task;
    }

    // Handle open.
    public Task<Unit> Handle(DidOpenTextDocumentParams openParams, CancellationToken token)
    {
        System.Diagnostics.Debug.WriteLine("documents", "Opening " + openParams.TextDocument.Uri);
        _documents.Add(new Document(openParams.TextDocument));
        return UpdateProjectAsync(openParams.TextDocument.Uri.ToUri());
    }

    // Handle change.
    public Task<Unit> Handle(DidChangeTextDocumentParams changeParams, CancellationToken token)
    {
        System.Diagnostics.Debug.WriteLine("documents", "Changing " + changeParams.TextDocument.Uri);
        var document = TextDocumentFromUri(changeParams.TextDocument.Uri.ToUri());
        foreach (var change in changeParams.ContentChanges)
        {
            int start = Extras.TextIndexFromPosition(document.Content, change.Range.Start);
            int length = Extras.TextIndexFromPosition(document.Content, change.Range.End) - start;

            StringBuilder rep = new StringBuilder(document.Content);
            rep.Remove(start, length);
            rep.Insert(start, change.Text);

            document.Update(rep.ToString(), change, changeParams.TextDocument.Version, _parserSettingsResolver.GetParserSettings(document.Uri));
        }
        return UpdateProjectAsync(document.Uri);
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

    public async Task WaitForCompilationAsync() => await Task.Run(() => _parseDone.Wait());

    public async Task<DeltinScript> OnScriptAvailabilityAsync()
    {
        await Task.WhenAny(_scriptReady.Task, Task.Delay(10000));
        return _languageServer.Compilation;
    }

    // todo: this does not need to return a task
    public Task AddDocumentAsync(Uri uri, string initialContent)
    {
        _documents.Add(new Document(uri, initialContent));
        return UpdateProjectAsync(uri);
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

    Task<Unit> UpdateProjectAsync(Uri uri) => UpdateProjectAsync(TextDocumentFromUri(uri));
    Task<Unit> UpdateProjectAsync(Document document)
    {
        _currentDocument = document;
        _wait.Set();
        return Task.FromResult(Unit.Value);
    }

    void SetupUpdateListener()
    {
        var stopToken = _stopUpdateListener.Token;
        _updateTask = Task.Run(() =>
        {
            while (!stopToken.IsCancellationRequested)
            {
                // If _wait is not signaled, signal _parseDone.
                if (!_wait.IsSet) _parseDone.Set();

                _wait.Wait();

                // Reset _wait so when _wait.Wait() is called, the task will pause.
                // If Parse() is called before the while loops, _wait.Wait() will be skipped and the document will be parsed again.
                _wait.Reset();

                // Make _parseDone wait.
                _parseDone.Reset();

                ExecUpdate(_currentDocument);
            }
        }, stopToken);
    }

    void ExecUpdate(Document item)
    {
        try
        {
            var settings = _projectSettings.GetProjectSettings(item.Uri);

            Diagnostics diagnostics = new Diagnostics();
            ScriptFile root = new ScriptFile(diagnostics, item);
            DeltinScript deltinScript = new DeltinScript(new TranslateSettings(diagnostics, root, _languageServer.FileGetter)
            {
                OutputLanguage = _languageServer.ConfigurationHandler.OutputLanguage,
                SourcedSettings = settings
            });
            _languageServer.Compilation = deltinScript;

            if (!_scriptReady.Task.IsCompleted)
                _scriptReady.SetResult(Unit.Value);

            // Publish result.
            var publishDiagnostics = diagnostics.GetPublishDiagnostics();

            if (deltinScript.WorkshopCode != null)
            {
                _documentEvent.Publish(deltinScript.WorkshopCode, deltinScript.ElementCount, publishDiagnostics);
            }
            else
            {
                _documentEvent.Publish(diagnostics.OutputDiagnostics(), -1, publishDiagnostics);
            }
        }
        catch (Exception ex)
        {
            _documentEvent.CompilationException(ex);
        }
    }

}