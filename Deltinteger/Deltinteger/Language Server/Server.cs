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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public ServerWorkspace Workspace { get; } = new ServerWorkspace();
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

            var builder = new LanguageServerBuilder(this);

            DocumentHandler = new DocumentHandler(builder);
            FileGetter = new FileGetter(DocumentHandler, builder.ParserSettingsResolver);
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
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Critical))
                .WithHandler<DocumentHandler>(DocumentHandler)
                .WithHandler<CompletionHandler>(completionHandler)
                .WithHandler<SignatureHandler>(signatureHandler)
                .WithHandler<ConfigurationHandler>(ConfigurationHandler)
                .WithHandler<DefinitionHandler>(definitionHandler)
                .WithHandler<HoverHandler>(hoverHandler)
                .WithHandler<ReferenceHandler>(referenceHandler)
                .WithHandler<CodeLensHandler>(codeLensHandler)
                .WithHandler<DoRenameHandler>(renameHandler)
                .WithHandler<DidChangeWatchedFilesHandler>(builder.FileHandlerBuilder.GetHandler())
                .WithHandler<PrepareRenameHandler>(prepareRenameHandler)
            ));

            Workspace.SetWorkspaceFolders(Server.ClientSettings.WorkspaceFolders);
            builder.ParserSettingsResolver.GetModuleFiles();
            
            Server.SendNotification(Version, Program.VERSION);            
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
                await DocumentHandler.WaitForParse();
                SemanticToken[] tokens = LastParse?.ScriptFromUri(new Uri(uriToken["fsPath"].ToObject<string>()))?.GetSemanticTokens();
                return tokens ?? new SemanticToken[0];
            }));

            // debugger start
            options.OnRequest<object>("debugger.start", args => Task.Run(() => {
                _debugger.Start();
                return new object();
            }));

            // debugger stop
            options.OnRequest<object>("debugger.stop", args => Task.Run(() => {
                _debugger.Stop();
                return new object();
            }));

            // debugger scopes
            options.OnRequest<ScopesArgs, DBPScope[]>("debugger.scopes", args => Task<DBPScope[]>.Run(() => {
                try
                {
                    if (_debugger.VariableCollection != null)
                        return _debugger.VariableCollection.GetScopes(args);
                }
                catch (Exception ex)
                {
                    DebuggerException(ex);
                }
                return new DBPScope[0];
            }));

            // debugger variables
            options.OnRequest<VariablesArgs, DBPVariable[]>("debugger.variables", args => Task<DBPVariable[]>.Run(() => {
                try
                {
                    if (_debugger.VariableCollection != null)
                        return _debugger.VariableCollection.GetVariables(args);
                }
                catch (Exception ex)
                {
                    DebuggerException(ex);
                }
                return new DBPVariable[0];
            }));

            // debugger evaluate
            options.OnRequest<EvaluateArgs, EvaluateResponse>("debugger.evaluate", args => Task<EvaluateResponse>.Run(() => {
                try
                {
                    return _debugger.VariableCollection?.Evaluate(args);
                }
                catch (Exception ex)
                {
                    DebuggerException(ex);
                    return EvaluateResponse.Empty;
                }
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

            // Decompile file
            options.OnRequest<DecompileFileArgs, object>("decompile.file", args => Task.Run<object>(() => {
                try
                {
                    // Parse the workshop code.
                    var tte = new ConvertTextToElement(Clipboard.GetText());
                    var workshop = tte.Get();

                    // Decompile the parsed workshop code.
                    var workshopToCode = new WorkshopDecompiler(workshop, new FileLobbySettingsResolver(args.File, workshop.LobbySettings), new CodeFormattingOptions());
                    string result = workshopToCode.Decompile();

                    // Create the file.
                    using (var writer = File.CreateText(args.File))
                        // Write the code to the file.
                        writer.Write(result);
                    
                    // Warning if the end of the file was not reached.
                    if (!tte.ReachedEnd)
                        return new {success = false, msg = "End of file not reached, stuck at: '" + tte.LocalStream.Substring(0, Math.Min(tte.LocalStream.Length, 50)) + "'" };
                    else
                        return new {success = true};
                }
                catch (Exception ex)
                {
                    return new {success = false, msg = ex.ToString()};
                }
            }));

            return options;
        }

        public void DebuggerException(Exception ex)
        {
            Server.SendNotification("debugger.error", ex.ToString());
        }

        class PathmapDocument
        {
            public string Text;
            public string File;

            public PathmapDocument() {}
        }

        class DecompileFileArgs
        {
            [JsonProperty("file")]
            public string File { get; set; }
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
            },
            new DocumentFilter() {
                Language = "ostw",
                Pattern = "dsconfig.json"
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