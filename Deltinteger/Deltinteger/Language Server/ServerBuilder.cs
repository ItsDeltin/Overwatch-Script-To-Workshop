namespace Deltin.Deltinteger.LanguageServer;

using Deltin.Deltinteger.LanguageServer.Model;
using Settings;
using Settings.TomlSettings;

public class LanguageServerBuilder
{
    public OstwLangServer Server { get; }
    public DidChangeWatchedFilesHandlerBuilder FileHandlerBuilder { get; }
    public ParserSettingsResolver ParserSettingsResolver { get; }
    public DsTomlWatcher ProjectSettings { get; }
    public ITomlDiagnosticReporter TomlDiagnosticsReporter { get; }
    public ILangLogger LangLogger { get; }

    public LanguageServerBuilder(OstwLangServer server, ITomlDiagnosticReporter tomlDiagnosticsReporter, ILangLogger langLogger)
    {
        Server = server;
        TomlDiagnosticsReporter = tomlDiagnosticsReporter;
        LangLogger = langLogger;
        FileHandlerBuilder = new DidChangeWatchedFilesHandlerBuilder();
        ParserSettingsResolver = new ParserSettingsResolver(this);
        ProjectSettings = new DsTomlWatcher(this);
    }
}