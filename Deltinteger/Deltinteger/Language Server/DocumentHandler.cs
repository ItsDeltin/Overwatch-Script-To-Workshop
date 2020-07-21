using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
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
        public List<TextDocumentItem> Documents { get; } = new List<TextDocumentItem>();
        private DeltintegerLanguageServer _languageServer { get; } 
        private SynchronizationCapability _compatibility;

        public DocumentHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
            _typeWaitTimer = new Timer(state => Update());
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "ostw");
        }

        // Text document registeration options.
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() =>  new TextDocumentRegistrationOptions() { 
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector 
        };

        // Save options.
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() => new TextDocumentSaveRegistrationOptions() {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
            IncludeText = _sendTextOnSave
        };

        // Document change options.
        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions() => new TextDocumentChangeRegistrationOptions() {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
            SyncKind = _syncKind
        };

        // Handle save.
        public Task<Unit> Handle(DidSaveTextDocumentParams saveParams, CancellationToken token)
        {
            if (_sendTextOnSave)
            {
                var document = TextDocumentFromUri(saveParams.TextDocument.Uri.ToUri());
                document.Text = saveParams.Text;
                return Parse(document);
            }
            else return Parse(saveParams.TextDocument.Uri.ToUri());
        }

        // Handle close.
        public Task<Unit> Handle(DidCloseTextDocumentParams closeParams, CancellationToken token)
        {
            Documents.Remove(TextDocumentFromUri(closeParams.TextDocument.Uri.ToUri()));
            return Unit.Task;
        }

        // Handle open.
        public Task<Unit> Handle(DidOpenTextDocumentParams openParams, CancellationToken token)
        {
            Documents.Add(openParams.TextDocument);
            return Parse(openParams.TextDocument.Uri.ToUri());
        }

        // Handle change.
        public Task<Unit> Handle(DidChangeTextDocumentParams changeParams, CancellationToken token)
        {
            var document = TextDocumentFromUri(changeParams.TextDocument.Uri.ToUri());
            foreach (var change in changeParams.ContentChanges)
            {
                int start = PosIndex(document.Text, change.Range.Start);
                int length = PosIndex(document.Text, change.Range.End) - start;

                StringBuilder rep = new StringBuilder(document.Text);
                rep.Remove(start, length);
                rep.Insert(start, change.Text);

                document.Text = rep.ToString();
                document.Version = changeParams.TextDocument.Version;
            }
            return Parse(document);
        }

        // Get client compatibility
        void ICapability<SynchronizationCapability>.SetCapability(SynchronizationCapability compatibility)
        {
            _compatibility = compatibility;
        }

        public TextDocumentItem TextDocumentFromUri(Uri uri)
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
        Task<Unit> Parse(TextDocumentItem document)
        {
            lock (_taskCompletionLock)
            {
                lock (_nextLock) _next = new TypeUpdateQueue(document);
                _typeWaitTimer.Change(_updateTaskIsRunning ? TimeToUpdate : 0, Timeout.Infinite);
            }

            return Unit.Task;
        }

        private const int TimeToUpdate = 250;
        private Timer _typeWaitTimer;
        private bool _updateTaskIsRunning = false;
        private TypeUpdateQueue _next;
        private object _taskCompletionLock = new object();
        private object _nextLock = new object();
        private List<TaskCompletionSource<int>> _onTypeCompleted = new List<TaskCompletionSource<int>>();

        void Update()
        {
            lock (_taskCompletionLock) _updateTaskIsRunning = true;

            try
            {
                Diagnostics diagnostics = new Diagnostics();
                ScriptFile root;
                lock (_nextLock)
                {
                    root = new ScriptFile(diagnostics, _next.ParseItem.Uri.ToUri(), _next.ParseItem.Text);
                    _next = null;
                }
                DeltinScript deltinScript = new DeltinScript(new TranslateSettings(diagnostics, root, _languageServer.FileGetter) {
                    OutputLanguage = _languageServer.ConfigurationHandler.OutputLanguage,
                    OptimizeOutput = _languageServer.ConfigurationHandler.OptimizeOutput
                });
                _languageServer.LastParse = deltinScript;

                // Publish the diagnostics.
                var publishDiagnostics = diagnostics.GetDiagnostics();
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
            finally
            {
                // Activate those waiting for the typing to complete
                lock (_taskCompletionLock)
                {
                    if (_next == null)
                        while (_onTypeCompleted.Count > 0)
                        {
                            _onTypeCompleted[0].SetResult(0);
                            _onTypeCompleted.RemoveAt(0);
                        }
                    _updateTaskIsRunning = false;
                }
            }
        }

        public async Task WaitForCompletedTyping(bool waitForScript = false)
        {
            var promise = new TaskCompletionSource<int>();

            lock (_taskCompletionLock)
            {
                lock (_nextLock)
                    if (_next == null && (!waitForScript || _languageServer.LastParse != null)) promise.SetResult(0);
                    else _onTypeCompleted.Add(promise);
            }

            Task completedTask = await Task.WhenAny(promise.Task, Task.Delay(10000));
            await completedTask;
        }
    }

    class TypeUpdateQueue
    {
        public TextDocumentItem ParseItem { get; }
        public TypeUpdateQueue(TextDocumentItem parseItem)
        {
            ParseItem = parseItem;
        }
    }
}