namespace Deltin.Deltinteger.LanguageServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Settings;
    using Settings.TomlSettings;
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;
    using OmniSharp.Extensions.LanguageServer.Protocol.Document;

    public class LanguageServerBuilder
    {
        public DeltintegerLanguageServer Server { get; }
        public DidChangeWatchedFilesHandlerBuilder FileHandlerBuilder { get; }
        public ParserSettingsResolver ParserSettingsResolver { get; }
        public DsTomlWatcher ProjectSettings { get; }
        public ITomlDiagnosticReporter TomlDiagnosticsReporter { get; }

        public LanguageServerBuilder(DeltintegerLanguageServer server)
        {
            Server = server;
            TomlDiagnosticsReporter = new TomlReporter(server);
            FileHandlerBuilder = new DidChangeWatchedFilesHandlerBuilder();
            ParserSettingsResolver = new ParserSettingsResolver(this);
            ProjectSettings = new DsTomlWatcher(this);
        }

        class TomlReporter : ITomlDiagnosticReporter
        {
            readonly DeltintegerLanguageServer server;
            public TomlReporter(DeltintegerLanguageServer server) => this.server = server;
            public void ReportDiagnostics(Uri uri, IEnumerable<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic> diagnostics)
            {
                server.Server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Uri = uri,
                    Diagnostics = diagnostics.ToArray()
                });
            }
        }
    }
}