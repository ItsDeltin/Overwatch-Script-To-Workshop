namespace Deltin.Deltinteger.LanguageServer;
using Settings;
using Settings.TomlSettings;

public class LanguageServerBuilder
{
    public OstwLangServer Server { get; }
    public DidChangeWatchedFilesHandlerBuilder FileHandlerBuilder { get; }
    public ParserSettingsResolver ParserSettingsResolver { get; }
    public DsTomlWatcher ProjectSettings { get; }
    public ITomlDiagnosticReporter TomlDiagnosticsReporter { get; }

    public LanguageServerBuilder(OstwLangServer server, ITomlDiagnosticReporter tomlDiagnosticsReporter)
    {
        Server = server;
        TomlDiagnosticsReporter = tomlDiagnosticsReporter;
        FileHandlerBuilder = new DidChangeWatchedFilesHandlerBuilder();
        ParserSettingsResolver = new ParserSettingsResolver(this);
        ProjectSettings = new DsTomlWatcher(this);
    }
}