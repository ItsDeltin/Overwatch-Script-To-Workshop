using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.LanguageServer.Settings;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using MediatR;

namespace Deltin.Deltinteger.LanguageServer
{
    public class DocumentHandler : ITextDocumentSyncHandler
    {
        // Static
        private static readonly TextDocumentSyncKind _syncKind = TextDocumentSyncKind.Incremental;
        private static readonly bool _sendTextOnSave = true;

        // Object
        public List<Document> Documents { get; } = new List<Document>();
        private readonly DeltintegerLanguageServer _languageServer;
        private readonly ParserSettingsResolver _parserSettingsResolver;
        private readonly DsTomlWatcher _projectSettings;
        private SynchronizationCapability _compatibility;
        private TaskCompletionSource<Unit> _scriptReady = new TaskCompletionSource<Unit>();

        public DocumentHandler(LanguageServerBuilder builder)
        {
            _languageServer = builder.Server;
            _parserSettingsResolver = builder.ParserSettingsResolver;
            _projectSettings = builder.ProjectSettings;
            SetupUpdateListener();
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new TextDocumentAttributes(uri, "ostw");

        // Document change
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentChangeRegistrationOptions()
        {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
            SyncKind = _syncKind
        };

        // Open
        TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentOpenRegistrationOptions()
        {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector
        };

        // Close
        TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentCloseRegistrationOptions()
        {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector
        };

        // Save
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSaveRegistrationOptions()
        {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector
        };

        // Handle save.
        public Task<Unit> Handle(DidSaveTextDocumentParams saveParams, CancellationToken token)
        {
            System.Diagnostics.Debug.WriteLine("documents", "Saving " + saveParams.TextDocument.Uri);
            if (_sendTextOnSave)
            {
                var document = TextDocumentFromUri(saveParams.TextDocument.Uri.ToUri());
                document.UpdateIfChanged(saveParams.Text, _parserSettingsResolver.GetParserSettings(document.Uri));
                return Parse(document);
            }
            else return Parse(saveParams.TextDocument.Uri.ToUri());
        }

        // Handle close.
        public Task<Unit> Handle(DidCloseTextDocumentParams closeParams, CancellationToken token)
        {
            System.Diagnostics.Debug.WriteLine("documents", "Closing " + closeParams.TextDocument.Uri);
            var removing = TextDocumentFromUri(closeParams.TextDocument.Uri.ToUri());
            removing.Remove();
            Documents.Remove(removing);
            return Unit.Task;
        }

        // Handle open.
        public Task<Unit> Handle(DidOpenTextDocumentParams openParams, CancellationToken token)
        {
            System.Diagnostics.Debug.WriteLine("documents", "Opening " + openParams.TextDocument.Uri);
            Documents.Add(new Document(openParams.TextDocument));
            return Parse(openParams.TextDocument.Uri.ToUri());
        }

        // Handle change.
        public Task<Unit> Handle(DidChangeTextDocumentParams changeParams, CancellationToken token)
        {
            System.Diagnostics.Debug.WriteLine("documents", "Changing " + changeParams.TextDocument.Uri);
            var document = TextDocumentFromUri(changeParams.TextDocument.Uri.ToUri());
            foreach (var change in changeParams.ContentChanges)
            {
                int start = PosIndex(document.Content, change.Range.Start);
                int length = PosIndex(document.Content, change.Range.End) - start;

                StringBuilder rep = new StringBuilder(document.Content);
                rep.Remove(start, length);
                rep.Insert(start, change.Text);

                document.Update(rep.ToString(), change, changeParams.TextDocument.Version, _parserSettingsResolver.GetParserSettings(document.Uri));
            }
            return Parse(document.Uri);
        }

        public Document TextDocumentFromUri(Uri uri)
        {
            for (int i = 0; i < Documents.Count; i++)
                // TODO-URI: Should use Uri.Compare? 
                if (Documents[i].Uri == uri)
                    return Documents[i];
            return null;
        }

        private static int PosIndex(string text, Position pos)
        {
            if (pos.Line == 0 && pos.Character == 0) return 0;

            int line = 0;
            int character = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    character = 0;
                }
                else
                {
                    character++;
                }

                if (pos.Line == line && pos.Character == character)
                    return i + 1;

                if (line > pos.Line)
                    throw new Exception();
            }
            throw new Exception();
        }

        Task<Unit> Parse(Uri uri) => Parse(TextDocumentFromUri(uri));
        Task<Unit> Parse(Document document)
        {
            _currentDocument = document;
            _wait.Set();
            return Task.FromResult(Unit.Value);
        }

        private Document _currentDocument;
        private ManualResetEventSlim _wait = new ManualResetEventSlim(false);
        private ManualResetEventSlim _parseDone = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource _stopUpdateListener = new CancellationTokenSource();

        public async Task WaitForParse() => await Task.Run(() => _parseDone.Wait());

        void SetupUpdateListener()
        {
            var stopToken = _stopUpdateListener.Token;
            Task.Run(() =>
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
                    Update(_currentDocument);
                }
            }, stopToken);
        }

        void Update(Document item)
        {
            try
            {
                Diagnostics diagnostics = new Diagnostics();
                ScriptFile root = new ScriptFile(diagnostics, item);
                DeltinScript deltinScript = new DeltinScript(new TranslateSettings(diagnostics, root, _languageServer.FileGetter)
                {
                    OutputLanguage = _languageServer.ConfigurationHandler.OutputLanguage,
                    OptimizeOutput = _languageServer.ConfigurationHandler.OptimizeOutput,
                    Settings = _projectSettings.GetProjectSettings(item.Uri)
                });
                _languageServer.LastParse = deltinScript;

                if (!_scriptReady.Task.IsCompleted)
                    _scriptReady.SetResult(Unit.Value);

                // Publish the diagnostics.
                var publishDiagnostics = diagnostics.GetPublishDiagnostics();
                foreach (var publish in publishDiagnostics)
                    _languageServer.Server.TextDocument.PublishDiagnostics(publish);

                if (deltinScript.WorkshopCode != null)
                {
                    _languageServer.Server.SendNotification(DeltintegerLanguageServer.SendWorkshopCode, deltinScript.WorkshopCode);
                    _languageServer.Server.SendNotification(DeltintegerLanguageServer.SendElementCount, deltinScript.ElementCount.ToString());
                }
                else
                {
                    _languageServer.Server.SendNotification(DeltintegerLanguageServer.SendWorkshopCode, diagnostics.OutputDiagnostics());
                    _languageServer.Server.SendNotification(DeltintegerLanguageServer.SendElementCount, "-");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An exception was thrown while parsing.");
                _languageServer.Server.SendNotification(DeltintegerLanguageServer.SendWorkshopCode, "An exception was thrown while parsing.\r\n" + ex.ToString());
                _languageServer.Server.SendNotification(DeltintegerLanguageServer.SendElementCount, "-");
            }
        }

        public async Task<DeltinScript> OnScriptAvailability()
        {
            await Task.WhenAny(_scriptReady.Task, Task.Delay(10000));
            return _languageServer.LastParse;
        }
    }
}