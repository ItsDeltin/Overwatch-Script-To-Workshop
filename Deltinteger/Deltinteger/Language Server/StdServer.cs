namespace Deltin.Deltinteger.LanguageServer;

using System.Threading.Tasks;
using System.IO;
using Serilog;
using Settings.TomlSettings;
using System;
using System.Collections.Generic;
using ProtocolServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Deltin.Deltinteger.LanguageServer.Model;
using PublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;

class StdServer : ITomlDiagnosticReporter, IDocumentEvent
{
    public const string SendWorkshopCode = "workshopCode";
    public const string SendElementCount = "elementCount";
    public const string Version = "version";

    public static void Run() => new StdServer().RunAsync().Wait();

    readonly OstwLangServer LangServer;
    ProtocolServer ProtocolServer;

    public StdServer()
    {
        LangServer = new OstwLangServer(this, this);
    }

    public async Task RunAsync()
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFile(), rollingInterval: RollingInterval.Day, flushToDiskInterval: new TimeSpan(0, 0, 10))
            .CreateLogger();

        ProtocolServer = await LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .ConfigureLogging(x => x
                .AddSerilog()
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Debug))
            .AddHandler(LangServer.DocumentHandler)
            .AddHandler(LangServer.CompletionHandler)
            .AddHandler(LangServer.SignatureHandler)
            .AddHandler(LangServer.ConfigurationHandler)
            .AddHandler(LangServer.DefinitionHandler)
            .AddHandler(LangServer.HoverHandler)
            .AddHandler(LangServer.ReferenceHandler)
            .AddHandler(LangServer.CodeLensHandler)
            .AddHandler(LangServer.RenameHandler)
            .AddHandler(LangServer.ColorHandler)
            .AddHandler(LangServer.SemanticTokenHandler)
            .AddHandler(LangServer.Builder.FileHandlerBuilder.GetHandler())
        );

        LangServer.Workspace.SetWorkspaceFolders(ProtocolServer.ClientSettings.WorkspaceFolders);
        LangServer.Builder.ProjectSettings.GetInitialFiles();
        LangServer.Builder.ParserSettingsResolver.GetInitialFiles();

        // ProtocolServer.SendNotification(Version, Program.VERSION);

        await ProtocolServer.WaitForExit;
    }

    private static string LogFile() => Path.Combine(Program.ExeFolder, "Log", "log.txt");

    // ~ ITomlDiagnosticReporter ~
    void ITomlDiagnosticReporter.ReportDiagnostics(Uri uri, IEnumerable<LspDiagnostic> diagnostics)
    {
        ProtocolServer.TextDocument.PublishDiagnostics(new()
        {
            Uri = uri,
            Diagnostics = new(diagnostics)
        });
    }

    // ~ IDocumentEvent ~
    public void Publish(string workshopCode, int elementCount, PublishDiagnosticsParams[] diagnostics)
    {
        ProtocolServer.SendNotification(SendWorkshopCode, workshopCode);
        ProtocolServer.SendNotification(SendElementCount, elementCount);

        foreach (var publish in diagnostics)
        {
            ProtocolServer.TextDocument.PublishDiagnostics(publish);
        }
    }

    public void CompilationException(Exception exception)
    {
        Publish("An exception was thrown while parsing.\r\n" + exception.ToString(), -1, Array.Empty<PublishDiagnosticsParams>());
        Serilog.Log.Error(exception, "An exception was thrown while parsing.");
    }
}