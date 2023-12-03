using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.LanguageServer.Settings.TomlSettings;
using Deltin.Deltinteger.LanguageServer.Model;
using Deltin.Deltinteger;
using LspSerializer = OmniSharp.Extensions.LanguageServer.Protocol.Serialization.LspSerializer;
using System.Linq;
using Deltin.Deltinteger.Decompiler;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Lobby;

// no namespace
#pragma warning disable CA1050
// disable 'Async' function name warning
#pragma warning disable VSTHRD200
// disable JSExport call site warning
#pragma warning disable CA1416
#nullable enable

/// <summary>
/// When OSTW is compiled for WASM to be run in the browser, Javascript can call the functions in this class to interact with OSTW.
/// See main.js
/// </summary>
public static partial class OstwJavascript
{
    static OstwLangServer? langServer;
    static TaskCompletionSource<bool> langServerStatus = new();
    static bool isStartingLanguageServer;

    // ~ Imported Javascript functions ~
    [JSImport("console.log", "main.js")]
    public static partial void ConsoleLog(string text);

    [JSImport("ostwWeb.getWorkshopElements", "main.js")]
    public static partial Task<string> GetWorkshopElements();

    [JSImport("ostwWeb.setDiagnostics", "main.js")]
    public static partial void SetDiagnostics(string publish);

    [JSImport("ostwWeb.setCompiledWorkshopCode", "main.js")]
    public static partial void SetCompiledWorkshopCode(string code, int elementCount);
    // ~ End Imported Javascript functions ~

    // ~ Exported functions ~
    [JSExport]
    public static async Task AddModelAsync(string uriStr, string content)
    {
        await EnsureServer();
        await langServer!.DocumentHandler.AddDocumentAsync(GetSystemUri(uriStr), content);
    }

    [JSExport]
    public static async Task UpdateModelAsync(string uriStr, string changes)
    {
        await EnsureServer();
        await langServer!.DocumentHandler.ChangeDocumentAsync(GetSystemUri(uriStr), FromJson<InterpChangeEvent[]>(changes));
    }

    [JSExport]
    public static async Task<string> GetCompletionAsync(string uriStr, int line, int character)
    {
        await EnsureServer();
        return ToJson((await langServer!.CompletionHandler.Handle(new()
        {
            TextDocument = GetDoc(uriStr),
            Position = GetPosition(line, character),
            Context = new() { },
        }, CancellationToken.None)).Select(completionItem => InterpCompletionItem.FromLsp(completionItem)));
    }

    [JSExport]
    public static async Task<string> GetSignatureHelpAsync(string uriStr, int line, int character, string context)
    {
        await EnsureServer();
        return ToJson(InterpSignatureHelp.FromLsp(await langServer!.SignatureHandler.Handle(new()
        {
            Context = JsonConvert.DeserializeObject<InterpSignatureContext>(context).ToLsp(),
            Position = new()
            {
                Character = character,
                Line = line
            },
            TextDocument = GetDoc(uriStr),
            WorkDoneToken = null
        }, CancellationToken.None)));
    }

    [JSExport]
    public static async Task<string> GetHoverAsync(string uriStr, int line, int character)
    {
        await EnsureServer();
        return ToJson(InterpHover.FromLsp(await langServer!.HoverHandler.Handle(new()
        {
            TextDocument = GetDoc(uriStr),
            Position = new()
            {
                Line = line,
                Character = character
            }
        }, CancellationToken.None)));
    }

    [JSExport]
    public static async Task<string> GetSemanticTokens(string uriStr, string? lastResultId)
    {
        await EnsureServer();
        var tokens = await langServer!.SemanticTokenHandler.Handle(new SemanticTokensParams()
        {
            TextDocument = GetDoc(uriStr),
            WorkDoneToken = null,
            PartialResultToken = null,
        }, CancellationToken.None);
        return ToJson(InterpSemantics.FromLsp(tokens));
    }

    [JSExport]
    public static string[] GetSemanticTokenTypes() => SemanticTokenHandler.SemanticTokenTypes;

    [JSExport]
    public static string[] GetSemanticTokenModifiers() => SemanticTokenHandler.SemanticTokenModifiers;

    [JSExport]
    public static string Decompile(string inputText) => ToJson(Decompiler.DecompileWorkshop(inputText));

    [JSExport]
    public static async Task Open(string uriStr)
    {
        await EnsureServer();
        var document = langServer!.DocumentHandler.TextDocumentFromUri(GetSystemUri(uriStr));
        if (document is null)
        {
            ConsoleLog($"Opened unregistered document '{uriStr}'");
            return;
        }
        langServer.ProjectUpdater.UpdateProject(document);
    }
    // ~ End Exported functions ~

    // ~ Helper functions ~
    static async Task EnsureServer()
    {
        if (isStartingLanguageServer)
        {
            await langServerStatus.Task;
        }
        else
        {
            isStartingLanguageServer = true;
            LoadData.LoadWith(await GetWorkshopElements());
            HeroSettingCollection.Init();
            ModeSettingCollection.Init();
            langServer = new OstwLangServer(
                tomlDiagnosticsReporter: new ITomlDiagnosticReporter.None(),
                documentEventHandler: StaticDocumentEventHandler.Instance,
                langLogger: ILangLogger.New(ConsoleLog),
                createFileGetter: (doc, settings) => new WebFileGetter(doc));
            langServerStatus.SetResult(true);
        }
    }

    static Position GetPosition(int line, int character) => new(line, character);

    static TextDocumentIdentifier GetDoc(string uriStr) => new(LspUri.From(uriStr));

    static Uri GetSystemUri(string uriStr) => new(uriStr);

    static string ToJson(object input) => JsonConvert.SerializeObject(input);
    static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json)!;
    // ~ End Helper Functions ~

    class StaticDocumentEventHandler : IDocumentEvent
    {
        public static readonly StaticDocumentEventHandler Instance = new();

        private StaticDocumentEventHandler() { }

        public void Publish(string workshopCode, int elementCount, PublishDiagnosticsParams[] diagnostics)
        {
            var publish = diagnostics.Select(modelDiagnostics => InterpScriptDiagnostics.FromLsp(modelDiagnostics)).ToArray();
            SetDiagnostics(JsonConvert.SerializeObject(publish));

            SetCompiledWorkshopCode(workshopCode, elementCount);
        }

        public void CompilationException(Exception exception)
        {
            SetCompiledWorkshopCode("An error occured while compiling: " + exception.ToString(), -1);
        }
    }
}