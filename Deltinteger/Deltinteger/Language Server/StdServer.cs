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
using Newtonsoft.Json;
using Deltin.Deltinteger.LanguageServer.Model;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using PublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;
using TextCopy;

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

        ProtocolServer = await LanguageServer.From(options => AddRequests(options
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
            .AddHandler(LangServer.Builder.FileHandlerBuilder.GetHandler()))
        );

        LangServer.Workspace.SetWorkspaceFolders(ProtocolServer.ClientSettings.WorkspaceFolders);
        LangServer.Builder.ProjectSettings.GetInitialFiles();
        LangServer.Builder.ParserSettingsResolver.GetInitialFiles();

        // ProtocolServer.SendNotification(Version, Program.VERSION);

        await ProtocolServer.WaitForExit;
    }

    private static string LogFile() => Path.Combine(Program.ExeFolder, "Log", "log.txt");

    class DecompileFileArgs
    {
        [JsonProperty("file")]
        public string File { get; set; }
    }
    private LanguageServerOptions AddRequests(LanguageServerOptions options) {
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
