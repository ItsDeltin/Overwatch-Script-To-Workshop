namespace Deltin.Deltinteger.LanguageServer;

using System;
using System.IO;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Pathfinder;
using Deltin.Deltinteger.Debugger;
using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using TextCopy;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Newtonsoft.Json;
using Settings.TomlSettings;
using Deltin.Deltinteger.LanguageServer.Model;
using Deltin.Deltinteger.LanguageServer.Settings;
using System.Linq;

public class OstwLangServer
{
    // ~ Protocol Handlers
    public DocumentHandler DocumentHandler { get; private set; }
    public CompletionHandler CompletionHandler { get; private set; }
    public SignatureHandler SignatureHandler { get; private set; }
    public DefinitionHandler DefinitionHandler { get; private set; }
    public HoverHandler HoverHandler { get; private set; }
    public ReferenceHandler ReferenceHandler { get; private set; }
    public CodeLensHandler CodeLensHandler { get; private set; }
    public DoRenameHandler RenameHandler { get; private set; }
    public ColorHandler ColorHandler { get; private set; }
    public SemanticTokenHandler SemanticTokenHandler { get; private set; }
    public DocumentSymbolsHandler DocumentSymbolHandler { get; private set; }
    // ~ End Protocol Handlers

    public LanguageServerBuilder Builder { get; }
    public IProjectUpdater ProjectUpdater { get; }
    public ServerWorkspace Workspace { get; } = new ServerWorkspace();
    public IFileGetter FileGetter { get; private set; }
    public ConfigurationHandler ConfigurationHandler { get; private set; }

    // Legacy
    // private readonly ClipboardListener _debugger;
    private Pathmap lastMap;

    public OstwLangServer(
        ITomlDiagnosticReporter tomlDiagnosticsReporter,
        IDocumentEvent documentEventHandler,
        ILangLogger langLogger = null,
        Func<DocumentHandler, IParserSettingsResolver, IFileGetter> createFileGetter = null,
        IDsSettingsProvider settingsProvider = null)
    {
        // _debugger = new ClipboardListener(this);
        createFileGetter ??= (doc, settings) => new LsFileGetter(doc, settings);

        Builder = new LanguageServerBuilder(this, tomlDiagnosticsReporter, langLogger ?? ILangLogger.Default, settingsProvider);

        var scriptCompiler = new ScriptCompiler(Builder, documentEventHandler);
        ProjectUpdater = new TimedProjectUpdater(scriptCompiler);

        DocumentHandler = new DocumentHandler(Builder);
        FileGetter = createFileGetter(DocumentHandler, Builder.ParserSettingsResolver);
        CompletionHandler = new CompletionHandler(this);
        SignatureHandler = new SignatureHandler(this);
        ConfigurationHandler = new ConfigurationHandler(this);
        DefinitionHandler = new DefinitionHandler(this);
        HoverHandler = new HoverHandler(this);
        ReferenceHandler = new ReferenceHandler(this);
        CodeLensHandler = new CodeLensHandler(this);
        RenameHandler = new DoRenameHandler(this);
        ColorHandler = new ColorHandler(this);
        SemanticTokenHandler = new SemanticTokenHandler(this);
        DocumentSymbolHandler = new(this);
    }

    private LanguageServerOptions AddRequests(LanguageServerOptions options)
    {
        // Pathmap creation is seperated into 2 requests, 'pathmapFromClipboard' and 'pathmapApply'.
        // Pathmap generation request.
        options.OnRequest<string>("pathmapFromClipboard", () => Task.Run(() =>
        {
            // Create the error handler for pathmap parser.
            ServerPathmapHandler error = new ServerPathmapHandler();

            // Get the pathmap. 'map' will be null if there is an error.
            try
            {
                Pathmap map = Pathmap.ImportFromActionSet(Clipboard.GetText(), error);

                if (map == null)
                    return error.Message;
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
                    return new PathmapEditorResult("An error was found in the pathmap script: " + compile.Diagnostics.GetErrors().First().ToString()); // error

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
            var compilation = await ProjectUpdater.GetProjectCompilationAsync();
            var tokens = compilation?.ScriptFromUri(new Uri(uriToken["fsPath"].ToObject<string>()))?.GetSemanticTokens();
            return tokens.ToArray() ?? [];
        }));

        // debugger start
        // options.OnRequest<object>("debugger.start", args => Task.Run(() =>
        // {
        //     _debugger.Start();
        //     return new object();
        // }));

        // debugger stop
        // options.OnRequest<object>("debugger.stop", args => Task.Run(() =>
        // {
        //     _debugger.Stop();
        //     return new object();
        // }));

        // debugger scopes
        // options.OnRequest<ScopesArgs, DBPScope[]>("debugger.scopes", args => Task<DBPScope[]>.Run(() =>
        // {
        //     try
        //     {
        //         if (_debugger.VariableCollection != null)
        //             return _debugger.VariableCollection.GetScopes(args);
        //     }
        //     catch (Exception ex)
        //     {
        //         DebuggerException(ex);
        //     }
        //     return new DBPScope[0];
        // }));

        // debugger variables
        // options.OnRequest<VariablesArgs, DBPVariable[]>("debugger.variables", args => Task<DBPVariable[]>.Run(() =>
        // {
        //     try
        //     {
        //         if (_debugger.VariableCollection != null)
        //             return _debugger.VariableCollection.GetVariables(args);
        //     }
        //     catch (Exception ex)
        //     {
        //         DebuggerException(ex);
        //     }
        //     return new DBPVariable[0];
        // }));

        // debugger evaluate
        // options.OnRequest<EvaluateArgs, EvaluateResponse>("debugger.evaluate", args => Task<EvaluateResponse>.Run(() =>
        // {
        //     try
        //     {
        //         return _debugger.VariableCollection?.Evaluate(args);
        //     }
        //     catch (Exception ex)
        //     {
        //         DebuggerException(ex);
        //         return EvaluateResponse.Empty;
        //     }
        // }));

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
        // ProtocolServer.SendNotification("debugger.error", ex.ToString());
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
        },
        new DocumentFilter()
        {
            Language = "ostw",
            Pattern = "ds.toml"
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