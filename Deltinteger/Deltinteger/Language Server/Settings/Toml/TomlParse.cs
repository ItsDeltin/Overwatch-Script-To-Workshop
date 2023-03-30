namespace Deltin.Deltinteger.LanguageServer.Settings.TomlSettings;
using System.Linq;
using System.Collections.Generic;
using Tomlyn;
using Deltin.Deltinteger.Model;
using PublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;
using LSDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LSRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using DiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

class TomlSettingsParser
{
    public static Result<T, IEnumerable<LSDiagnostic>> Parse<T>(string text) where T : class, new()
    {
        var syntax = Toml.Parse(text);

        // Return syntax errors.
        if (syntax.HasErrors)
            return Result<T, IEnumerable<LSDiagnostic>>.Error(TomlDiagnosticToLangServerDiagnostic(syntax.Diagnostics));

        // Convert to model.
        if (syntax.TryToModel(out T model, out var modelDiagnostics))
            return Result<T, IEnumerable<LSDiagnostic>>.Ok(model);

        else
            return Result<T, IEnumerable<LSDiagnostic>>.Error(TomlDiagnosticToLangServerDiagnostic(modelDiagnostics));
    }

    static IEnumerable<LSDiagnostic> TomlDiagnosticToLangServerDiagnostic(Tomlyn.Syntax.DiagnosticsBag diagnostics)
    {
        return from d in diagnostics
               select new LSDiagnostic()
               {
                   Severity = d.Kind == Tomlyn.Syntax.DiagnosticMessageKind.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                   Message = d.Message,
                   Range = new LSRange(
                    d.Span.Start.Line,
                    d.Span.Start.Column,
                    d.Span.End.Line,
                    d.Span.End.Column
                   )
               };
    }
}