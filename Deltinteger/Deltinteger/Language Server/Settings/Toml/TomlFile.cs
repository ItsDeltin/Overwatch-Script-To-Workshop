using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LSDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;

namespace Deltin.Deltinteger.LanguageServer.Settings.TomlSettings;

public class TomlFile<T> : ISettingFile where T : class, new()
{
    public Uri Uri { get; }
    public T Settings { get; private set; }
    readonly ITomlDiagnosticReporter diagnosticReporter;

    public TomlFile(Uri uri, ITomlDiagnosticReporter diagnosticReporter)
    {
        Uri = uri;
        this.diagnosticReporter = diagnosticReporter;
        Update();
    }

    public void Update()
    {
        string content;
        try
        {
            // Attempt to read the settings file.
            content = File.ReadAllText(Uri.LocalPath);
        }
        catch (Exception ex)
        {
            // TODO: add diagnostics for user
            return;
        }
        // Parse settings.
        TomlSettingsParser.Parse<T>(content).Match(
            ok =>
            {
                Settings = ok;
                diagnosticReporter.ReportDiagnostics(Uri, Enumerable.Empty<LSDiagnostic>());
            },
            error =>
            {
                Settings = default(T);
                diagnosticReporter.ReportDiagnostics(Uri, error);
            }
        );
    }
}

/// <summary>Used by the TomlFile class to report diagnostics</summary>
public interface ITomlDiagnosticReporter
{
    void ReportDiagnostics(Uri uri, IEnumerable<LSDiagnostic> diagnostics);

    public record None() : ITomlDiagnosticReporter
    {
        public void ReportDiagnostics(Uri uri, IEnumerable<LSDiagnostic> diagnostics)
        {
        }
    }
}
