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
using MediatR;
using DS.Analysis;

namespace Deltin.Deltinteger.LanguageServer
{
    class DeltintegerLanguageServer
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
        public DeltinScript LastParse
        {
            get
            {
                lock (_lastParseLock) return _lastParse;
            }
            set
            {
                lock (_lastParseLock) _lastParse = value;
            }
        }

        public DocumentHandler DocumentHandler { get; private set; }
        public FileGetter FileGetter { get; private set; }
        public ConfigurationHandler ConfigurationHandler { get; private set; }
        private readonly ClipboardListener _debugger;
        private Pathmap lastMap;

        public DSAnalysis Analysis { get; } = new DSAnalysis();

        public DeltintegerLanguageServer()
        {
            _debugger = new ClipboardListener(this);
        }

        async Task RunServer()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(LogFile(), rollingInterval: RollingInterval.Day, flushToDiskInterval: new TimeSpan(0, 0, 10))
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
            ColorHandler colorHandler = new ColorHandler(this);

            Server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => AddRequests(options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(x => x
                    .AddSerilog()
                    .AddLanguageProtocolLogging()
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug))
                .AddHandler(DocumentHandler)
            // .AddHandler(completionHandler)
            // .AddHandler(signatureHandler)
            // .AddHandler(ConfigurationHandler)
            // .AddHandler(definitionHandler)
            // .AddHandler(hoverHandler)
            // .AddHandler(referenceHandler)
            // .AddHandler(codeLensHandler)
            // .AddHandler(renameHandler)
            // .AddHandler(colorHandler)
            ));

            Server.SendNotification(Version, Program.VERSION);
            await Server.WaitForExit;
        }

        private LanguageServerOptions AddRequests(LanguageServerOptions options)
        {
            // Pathmap creation is seperated into 2 requests, 'pathmapFromClipboard' and 'pathmapApply'.
            // Pathmap generation request.
            options.OnRequest<object, string>("pathmapFromClipboard", _ => Task<string>.Run(() =>
            {
                // Create the error handler for pathmap parser.
                ServerPathmapHandler error = new ServerPathmapHandler();

                // Get the pathmap. 'map' will be null if there is an error.
                try
                {
                    Pathmap map = Pathmap.ImportFromActionSet(Clipboard.GetText(), error);

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
            options.OnRequest<Newtonsoft.Json.Linq.JToken>("pathmapApply", uriToken => Task.Run(() =>
            {
                // Save 'lastMap' to a file.
                string result = lastMap.ExportAsJSON();
                string output = uriToken["path"].ToObject<string>().Trim('/');
                using (var stream = new StreamWriter(output))
                    stream.Write(result);
            }));

            // Pathmap editor request.
            options.OnRequest<PathmapDocument, PathmapEditorResult>("pathmapEditor", (editFileToken) => Task<PathmapEditorResult>.Run(() =>
            {
                try
                {
                    DeltinScript compile;
                    if (editFileToken.Text == null)
                    {
                        string editor = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");
                        compile = new DeltinScript(new TranslateSettings(editor)
                        {
                            OutputLanguage = ConfigurationHandler.OutputLanguage
                        });
                    }
                    else
                    {
                        compile = Editor.Generate(editFileToken.File, Pathmap.ImportFromText(editFileToken.Text), ConfigurationHandler.OutputLanguage);
                    }

                    if (compile.Diagnostics.ContainsErrors())
                        return new PathmapEditorResult("An error was found in the pathmap script: " + compile.Diagnostics.GetDiagnostics()[0].ToString()); // error

                    Clipboard.SetText(compile.WorkshopCode);
                    return new PathmapEditorResult(); // success
                }
                catch (Exception ex)
                {
                    return new PathmapEditorResult(ex.Message);
                }
            }));

            // semantic tokens
            options.OnRequest<Newtonsoft.Json.Linq.JToken, SemanticToken[]>("semanticTokens", (uriToken) => Task<SemanticToken[]>.Run(async () =>
            {
                await DocumentHandler.WaitForParse();
                SemanticToken[] tokens = LastParse?.ScriptFromUri(new Uri(uriToken["fsPath"].ToObject<string>()))?.GetSemanticTokens();
                return tokens ?? new SemanticToken[0];
            }));

            // debugger start
            options.OnRequest<object>("debugger.start", args => Task.Run(() =>
            {
                _debugger.Start();
                return new object();
            }));

            // debugger stop
            options.OnRequest<object>("debugger.stop", args => Task.Run(() =>
            {
                _debugger.Stop();
                return new object();
            }));

            // debugger scopes
            options.OnRequest<ScopesArgs, DBPScope[]>("debugger.scopes", args => Task<DBPScope[]>.Run(() =>
            {
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
            options.OnRequest<VariablesArgs, DBPVariable[]>("debugger.variables", args => Task<DBPVariable[]>.Run(() =>
            {
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
            options.OnRequest<EvaluateArgs, EvaluateResponse>("debugger.evaluate", args => Task<EvaluateResponse>.Run(() =>
            {
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
            options.OnRequest<DecompileResult>("decompile.insert", () => Task<DecompileResult>.Run(() =>
            {
                try
                {
                    var tte = new ConvertTextToElement(Clipboard.GetText());
                    var workshop = tte.Get();
                    var code = new WorkshopDecompiler(workshop, new OmitLobbySettingsResolver(), new CodeFormattingOptions()).Decompile();
                    return new DecompileResult(tte, code);
                }
                catch (Exception ex)
                {
                    return new DecompileResult(ex);
                }
            }));

            // Decompile file
            options.OnRequest<DecompileFileArgs, DecompileResult>("decompile.file", args => Task.Run<DecompileResult>(() =>
            {
                try
                {
                    // Parse the workshop code.
                    var tte = new ConvertTextToElement(Clipboard.GetText());
                    var workshop = tte.Get();

                    // Decompile the parsed workshop code.
                    var workshopToCode = new WorkshopDecompiler(workshop, new FileLobbySettingsResolver(args.File, workshop.LobbySettings), new CodeFormattingOptions());
                    var code = workshopToCode.Decompile();

                    var result = new DecompileResult(tte, code);

                    // Only create the decompile was successful.
                    if (result.Success)
                        // Create the file.
                        using (var writer = File.CreateText(args.File))
                            // Write the code to the file.
                            writer.Write(code);

                    return result;
                }
                catch (Exception ex)
                {
                    return new DecompileResult(ex);
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

            public PathmapDocument() { }
        }

        class DecompileFileArgs
        {
            [JsonProperty("file")]
            public string File { get; set; }
        }

        public static readonly DocumentSelector DocumentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Language = "ostw",
                Pattern = "**/*.del"
            },
            new DocumentFilter()
            {
                Language = "ostw",
                Pattern = "**/*.ostw"
            },
            new DocumentFilter()
            {
                Language = "ostw",
                Pattern = "**/*.workshop"
            }
        );

        Exception currentException;

        public Task<Unit> Execute(Func<Task<Unit>> op)
        {
            if (currentException != null)
            {
                System.Diagnostics.Debug.Fail(op.ToString());
                return Unit.Task;
            }

            try
            {
                return op();
            }
            catch (Exception ex)
            {
                currentException = ex;
                return Unit.Task;
            }
        }
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
