namespace Deltin.Deltinteger.LanguageServer;

using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.LanguageServer.Model;
using Deltin.Deltinteger.LanguageServer.Settings;

interface IScriptCompiler
{
    void Compile(Document triggerDocument);
    DeltinScript Current();
}

class ScriptCompiler : IScriptCompiler
{
    readonly IDocumentEvent documentEvent;
    readonly DsTomlWatcher projectSettings;
    readonly OstwLangServer languageServer;
    DeltinScript deltinScript;

    public ScriptCompiler(LanguageServerBuilder builder, IDocumentEvent documentEvent)
    {
        this.documentEvent = documentEvent;
        projectSettings = builder.ProjectSettings;
        languageServer = builder.Server;
    }

    public void Compile(Document triggerDocument)
    {
        try
        {
            var settings = projectSettings.GetProjectSettings(triggerDocument.Uri);

            Diagnostics diagnostics = new Diagnostics();
            ScriptFile root = new ScriptFile(diagnostics, triggerDocument);
            deltinScript = new DeltinScript(new TranslateSettings(diagnostics, root, languageServer.FileGetter)
            {
                OutputLanguage = languageServer.ConfigurationHandler.OutputLanguage,
                SourcedSettings = settings
            });

            // Publish result.
            var publishDiagnostics = diagnostics.GetPublishDiagnostics();

            if (deltinScript.WorkshopCode != null)
            {
                documentEvent.Publish(deltinScript.WorkshopCode, deltinScript.ElementCount, publishDiagnostics);
            }
            else
            {
                documentEvent.Publish(diagnostics.OutputDiagnostics(), -1, publishDiagnostics);
            }
        }
        catch (Exception ex)
        {
            documentEvent.CompilationException(ex);
        }
    }

    public DeltinScript Current() => deltinScript;
}