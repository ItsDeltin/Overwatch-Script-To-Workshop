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

// no namespace

// disable 'Async' function name warning
#pragma warning disable VSTHRD200

public static partial class OstwJavascript
{
    static OstwLangServer langServer;
    static StaticDocumentEventHandler eventHandler;

    // ~ Imported Javascript functions ~
    [JSImport("console.log", "main.js")]
    public static partial void ConsoleLog(string text);

    [JSImport("ostwWeb.getWorkshopElements", "main.js")]
    public static partial Task<string> GetWorkshopElements();

    [JSImport("ostwWeb.setDiagnostics", "main.js")]
    public static partial void SetDiagnostics(string publish);
    // ~ End Imported Javascript functions ~

    // ~ Exported functions ~
    [JSExport]
    public static async Task AddModelAsync(string uriStr, string content)
    {
        await EnsureServer();
        await langServer.DocumentHandler.AddDocumentAsync(GetSystemUri(uriStr), content);
    }

    [JSExport]
    public static async Task UpdateModelAsync(string uriStr, string changes)
    {
        await EnsureServer();
        await langServer.DocumentHandler.ChangeDocumentAsync(GetSystemUri(uriStr), FromJson<InterpChangeEvent[]>(changes));
    }

    [JSExport]
    public static async Task<string> GetCompletionAsync(string uriStr, int line, int character)
    {
        await EnsureServer();
        return ToJson((await langServer.CompletionHandler.Handle(new()
        {
            TextDocument = GetDoc(uriStr),
            Position = GetPosition(line, character),
            Context = new() { },
        }, CancellationToken.None)).Select(completionItem => InterpCompletionItem.FromLsp(completionItem)));
    }
    // ~ End Exported functions ~

    // ~ Helper functions ~
    static async Task EnsureServer()
    {
        if (langServer != null)
        {
            return;
        }
        ConsoleLog("Setting up the OSTW compiler");
        LoadData.LoadWith(await GetWorkshopElements());
        eventHandler = new StaticDocumentEventHandler();
        langServer = new OstwLangServer(new ITomlDiagnosticReporter.None(), eventHandler);
    }

    static Position GetPosition(int line, int character) => new(line, character);

    static TextDocumentIdentifier GetDoc(string uriStr) => new(LspUri.From(uriStr));

    static Uri GetSystemUri(string uriStr) => new(uriStr);

    static string ToJson(object input) => LspSerializer.Instance.SerializeObject(input);
    static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json);
    // ~ End Helper Functions ~

    class StaticDocumentEventHandler : IDocumentEvent
    {
        public void Publish(string workshopCode, int elementCount, PublishDiagnosticsParams[] diagnostics)
        {
            ConsoleLog("Workshop code: " + workshopCode);

            var publish = diagnostics.Select(modelDiagnostics => InterpScriptDiagnostics.FromLsp(modelDiagnostics)).ToArray();
            SetDiagnostics(JsonConvert.SerializeObject(publish));
        }

        public void CompilationException(Exception exception)
        {
            ConsoleLog("Internal compilation error: " + exception.ToString());
        }
    }
}