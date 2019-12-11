using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Deltin.Deltinteger.Parse;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

using ProtocolRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

using MediatR;
using Serilog;

namespace Deltin.Deltinteger.LanguageServer
{
    public class DeltintegerLanguageServer
    {
        public static void Run()
        {
            new DeltintegerLanguageServer();
        }

        private static string LogFile() => Path.Combine(Program.ExeFolder, "Log", "log.txt");

        private DeltintegerLanguageServer()
        {
            RunServer().Wait();
        }

        public ILanguageServer Server { get; private set; }

        private DeltinScript _lastParse = null;
        private object _lastParseLock = new object();
        public DeltinScript LastParse {
            get {
                lock (_lastParseLock) return _lastParse;
            }
            set {
                lock (_lastParseLock) _lastParse = value;
            }
        }

        async Task RunServer()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(LogFile(), rollingInterval: RollingInterval.Day, flushToDiskInterval:new TimeSpan(0, 0, 10))
                .CreateLogger();
            
            Serilog.Log.Information("Deltinteger Language Server");

            DocumentHandler documentHandler = new DocumentHandler(this);
            CompletionHandler completionHandler = new CompletionHandler(this);

            Server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddSerilog()
                    .AddLanguageServer()
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug))
                .WithHandler<DocumentHandler>(documentHandler)
                .WithHandler<CompletionHandler>(completionHandler));
            
            await Server.WaitForExit;
        }
    }

    class DocumentHandler : ITextDocumentSyncHandler
    {
        // Static
        private static readonly TextDocumentSyncKind _syncKind = TextDocumentSyncKind.Incremental;
        private static readonly bool _sendTextOnSave = true;
        public static readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.del"
            }
        );

        // Object
        public List<TextDocumentItem> Documents { get; } = new List<TextDocumentItem>();
        private DeltintegerLanguageServer _languageServer { get; } 
        private SynchronizationCapability _compatibility;

        public DocumentHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            // TODO: confirm if this is correct
            return new TextDocumentAttributes(uri, "ostw");
        }

        // Text document registeration options.
        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions() =>  new TextDocumentRegistrationOptions() { 
            DocumentSelector = _documentSelector 
        };

        // Save options.
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() => new TextDocumentSaveRegistrationOptions() {
            DocumentSelector = _documentSelector,
            IncludeText = _sendTextOnSave
        };

        // Document change options.
        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions() => new TextDocumentChangeRegistrationOptions() {
            DocumentSelector = _documentSelector,
            SyncKind = _syncKind
        };

        // Handle save.
        public Task<Unit> Handle(DidSaveTextDocumentParams saveParams, CancellationToken token)
        {
            if (_sendTextOnSave)
            {
                var document = TextDocumentFromUri(saveParams.TextDocument.Uri);
                document.Text = saveParams.Text;
                Parse(document);
            }
            else Parse(saveParams.TextDocument.Uri);
            return Unit.Task;
        }

        // Handle close.
        public Task<Unit> Handle(DidCloseTextDocumentParams closeParams, CancellationToken token)
        {
            Documents.Remove(TextDocumentFromUri(closeParams.TextDocument.Uri));
            return Unit.Task;
        }

        // Handle open.
        public Task<Unit> Handle(DidOpenTextDocumentParams openParams, CancellationToken token)
        {
            Documents.Add(openParams.TextDocument);
            Parse(openParams.TextDocument.Uri);
            return Unit.Task;
        }

        // Handle change.
        public Task<Unit> Handle(DidChangeTextDocumentParams changeParams, CancellationToken token)
        {
            var document = TextDocumentFromUri(changeParams.TextDocument.Uri);
            foreach (var change in changeParams.ContentChanges)
            {
                int start = PosIndex(document.Text, change.Range.Start);
                int length = PosIndex(document.Text, change.Range.End) - start;

                StringBuilder rep = new StringBuilder(document.Text);
                rep.Remove(start, length);
                rep.Insert(start, change.Text);

                document.Text = rep.ToString();
            }
            Parse(document);
            return Unit.Task;
        }

        // Get client compatibility
        void ICapability<SynchronizationCapability>.SetCapability(SynchronizationCapability compatibility)
        {
            _compatibility = compatibility;
        }

        public TextDocumentItem TextDocumentFromUri(Uri uri)
        {
            for (int i = 0; i < Documents.Count; i++)
                if (Documents[i].Uri == uri)
                    return Documents[i];
            return null;
        }

        private static int PosIndex(string text, Position pos)
        {
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

            // string[] lineSplit = text.Split('\n');

            // int index = 0;
            // for (int i = 0; i < pos.Line; i++) index += lineSplit[i].Length;
            // index += (int)pos.Character;
            
            // return index;
        }

        void Parse(Uri uri) => Parse(TextDocumentFromUri(uri));
        void Parse(TextDocumentItem document)
        {
            try
            {
                Diagnostics diagnostics = new Diagnostics();
                ScriptFile root = new ScriptFile(diagnostics, document.Uri, document.Text);
                DeltinScript deltinScript = new DeltinScript(diagnostics, root);
                _languageServer.LastParse = deltinScript;

                var publishDiagnostics = diagnostics.GetDiagnostics();
                foreach (var publish in publishDiagnostics)
                    _languageServer.Server.Document.PublishDiagnostics(publish);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An exception was thrown while parsing.");
            }
        }
    }

    class CompletionHandler : ICompletionHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public CompletionHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<CompletionList> Handle(CompletionParams completionParams, CancellationToken token)
        {
            // TODO: Fill defaultCompletion with the default workshop methods and enums. 
            CompletionList defaultCompletion = new CompletionList(new CompletionItem() { Label = "wow this is cool", Kind = CompletionItemKind.Variable });

            // If the script has not been parsed yet, return the default completion.
            if (_languageServer.LastParse == null) return defaultCompletion;

            // Get the script from the uri. If it isn't parsed, return the default completion. 
            var script = _languageServer.LastParse.ScriptFromUri(completionParams.TextDocument.Uri);
            if (script == null) return defaultCompletion;

            var completions = script.GetCompletionRanges();
            List<CompletionRange> inRange = new List<CompletionRange>();
            foreach (var completion in completions)
                if (completion.Range.IsInside(completionParams.Position))
                    inRange.Add(completion);
            
            if (inRange.Count > 0)
            {
                inRange = inRange
                    // Order by if the completion range has priority. True is first.
                    .OrderByDescending(range => range.Priority)
                    // Then order by the size of the ranges.
                    .ThenBy(range => range.Range)
                    .ToList();
                
                if (inRange[0].Priority)
                    inRange.RemoveRange(1, inRange.Count - 2);
                
                List<CompletionItem> items = new List<CompletionItem>();
                foreach (var range in inRange)
                    items.AddRange(range.Scope.GetCompletion(completionParams.Position));
                
                return new CompletionList(items);
            }
            else return defaultCompletion;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = DocumentHandler._documentSelector,
                // Most tools trigger completion request automatically without explicitly requesting
                // it using a keyboard shortcut (e.g. Ctrl+Space). Typically they do so when the user
                // starts to type an identifier. For example if the user types `c` in a JavaScript file
                // code complete will automatically pop up present `console` besides others as a
                // completion item. Characters that make up identifiers don't need to be listed here.
                //
                // If code complete should automatically be trigger on characters not being valid inside
                // an identifier (for example `.` in JavaScript) list them in `triggerCharacters`.
                TriggerCharacters = new Container<string>("."),
                // The server provides support to resolve additional
                // information for a completion item.
                ResolveProvider = false
            };
        }

        // Client compatibility
        private CompletionCapability _capability;
        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }
    }
}