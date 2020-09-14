using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Pathfinder;
using Deltin.Deltinteger.Debugger;
using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using Serilog;
using TextCopy;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.LanguageServer
{
    public class DeltintegerLanguageServer
    {
        public const string SendWorkshopCode = "workshopCode";
        public const string SendElementCount = "elementCount";
        public const string Version = "version";

        public static void Run()
        {
            new DeltintegerLanguageServer().RunServer().Wait();
        }

        private static string LogFile() => Path.Combine(Program.ExeFolder, "Log", "log.txt");

        public OmniSharp.Extensions.LanguageServer.Server.LanguageServer Server { get; private set; }

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

        public DocumentHandler DocumentHandler { get; private set; }
        public FileGetter FileGetter { get; private set; }
        public ConfigurationHandler ConfigurationHandler { get; private set; }
        private readonly ClipboardListener _debugger;
        private Pathmap lastMap;

        public DeltintegerLanguageServer()
        {
            _debugger = new ClipboardListener(this);
        }

        async Task RunServer()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(LogFile(), rollingInterval: RollingInterval.Day, flushToDiskInterval:new TimeSpan(0, 0, 10))
                .CreateLogger();
            
            Serilog.Log.Information("Deltinteger Language Server");

            DocumentHandler = new DocumentHandler(this);
            FileGetter = new FileGetter(DocumentHandler);
            CompletionHandler completionHandler = new CompletionHandler(this);
            SignatureHandler signatureHandler = new SignatureHandler(this);
            ConfigurationHandler = new ConfigurationHandler(this);
            DefinitionHandler definitionHandler = new DefinitionHandler(this);
            HoverHandler hoverHandler = new HoverHandler(this);
            ReferenceHandler referenceHandler = new ReferenceHandler(this);
            CodeLensHandler codeLensHandler = new CodeLensHandler(this);
            DoRenameHandler renameHandler = new DoRenameHandler(this);
            PrepareRenameHandler prepareRenameHandler = new PrepareRenameHandler(this);

            Server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => AddRequests(options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddSerilog()
                    .AddLanguageProtocolLogging()
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug))
                .WithHandler<DocumentHandler>(DocumentHandler)
                .WithHandler<CompletionHandler>(completionHandler)
                .WithHandler<SignatureHandler>(signatureHandler)
                .WithHandler<ConfigurationHandler>(ConfigurationHandler)
                .WithHandler<DefinitionHandler>(definitionHandler)
                .WithHandler<HoverHandler>(hoverHandler)
                .WithHandler<ReferenceHandler>(referenceHandler)
                .WithHandler<CodeLensHandler>(codeLensHandler)
                .WithHandler<DoRenameHandler>(renameHandler)
                .WithHandler<PrepareRenameHandler>(prepareRenameHandler)                
            ));
            
            Server.SendNotification(Version, Program.VERSION);
            
            Task wait = _debugger.Listen();
            
            await Server.WaitForExit;
        }

        private LanguageServerOptions AddRequests(LanguageServerOptions options)
        {
            // Pathmap creation is seperated into 2 requests, 'pathmapFromClipboard' and 'pathmapApply'.
            // Pathmap generation request.
            options.OnRequest<object, string>("pathmapFromClipboard", _=> Task<string>.Run(() => {
                // Create the error handler for pathmap parser.
                ServerPathmapHandler error = new ServerPathmapHandler();

                // Get the pathmap. 'map' will be null if there is an error.
                try
                {
                    Pathmap map = Pathmap.ImportFromCSV(Clipboard.GetText(), error);

                    if (map == null) return error.Message;
                    else
                    {
                        lastMap = map;
                        return "success";
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }));

            // Pathmap save request.
            options.OnRequest<Newtonsoft.Json.Linq.JToken>("pathmapApply", uriToken => Task.Run(() => {
                
                // Save 'lastMap' to a file.
                string result = lastMap.ExportAsJSON();
                string output = uriToken["path"].ToObject<string>().Trim('/');
                using (var stream = new StreamWriter(output))
                    stream.Write(result);
            }));

            // Pathmap editor request.
            options.OnRequest<PathmapDocument, bool>("pathmapEditor", (editFileToken) => Task<bool>.Run(() => {

                DeltinScript compile;
                if (editFileToken.Text == null)
                {
                    string editor = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");
                    compile = new DeltinScript(new TranslateSettings(editor) {
                        OutputLanguage = ConfigurationHandler.OutputLanguage
                    });
                }
                else
                {
                    compile = Editor.Generate(editFileToken.File, Pathmap.ImportFromText(editFileToken.Text), ConfigurationHandler.OutputLanguage);
                }

                Clipboard.SetText(compile.WorkshopCode);

                return true;
            }));

            // semantic tokens
            options.OnRequest<Newtonsoft.Json.Linq.JToken, SemanticToken[]>("semanticTokens", (uriToken) => Task<SemanticToken[]>.Run(async () => 
            {
                SemanticToken[] tokens = (await DocumentHandler.OnScriptAvailability())?.ScriptFromUri(new Uri(uriToken["fsPath"].ToObject<string>()))?.GetSemanticTokens();
                return tokens ?? new SemanticToken[0];
            }));

            // debugger scopes
            options.OnRequest<ScopesArgs, DBPScope[]>("debugger.scopes", args => Task<DBPScope[]>.Run(() => {
                if (_debugger.VariableCollection != null)
                    return _debugger.VariableCollection.GetScopes(args);
                return null;
            }));

            // debugger variables
            options.OnRequest<VariablesArgs, DBPVariable[]>("debugger.variables", args => Task<DBPVariable[]>.Run(() => {
                if (_debugger.VariableCollection != null)
                    return _debugger.VariableCollection.GetVariables(args);
                return null;
            }));

            // debugger evaluate
            options.OnRequest<EvaluateArgs, EvaluateResponse>("debugger.evaluate", args => Task<EvaluateResponse>.Run(() => {
                return _debugger.VariableCollection?.Evaluate(args);
            }));
            
            // Decompile insert
            options.OnRequest<object>("decompile.insert", () => Task.Run(() =>
            {
                try
                {
                    var workshop = new ConvertTextToElement(Clipboard.GetText()).Get();
                    var code = new WorkshopDecompiler(workshop, new OmitLobbySettingsResolver(), new CodeFormattingOptions()).Decompile();
                    object result = new {success = true, code = code};
                    return result;
                }
                catch (Exception ex)
                {
                    object result = new {success = false, code = ex.ToString()};
                    return result;
                }
            }));

            return options;
        }

        class PathmapDocument
        {
            public string Text;
            public string File;

            public PathmapDocument() {}
        }

        public static readonly DocumentSelector DocumentSelector = new DocumentSelector(
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.del"
            },
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.ostw"
            },
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "**/*.workshop"
            }
        );
    }

    class ServerPathmapHandler : IPathmapErrorHandler
    {
        public string Message { get; private set; }

        public void Error(string error)
        {
            Message = error;
        }
    }
}