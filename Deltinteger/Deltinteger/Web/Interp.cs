namespace Deltin.Deltinteger.LanguageServer.Model;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspPositition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LspPublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;
using System;
using System.Linq;

// This file recreates some of the LSP protocol as structs so
// that they can be used with JsExport

public record struct InterpChangeEvent(InterpRange range, int rangeLength, string text);

public record struct InterpRange(InterpPosition start, InterpPosition end)
{
    public static implicit operator LspRange(InterpRange interpRange) => new()
    {
        Start = interpRange.start,
        End = interpRange.end
    };
    public static implicit operator InterpRange(LspRange lspRange) => new(lspRange.Start, lspRange.End);
}

public record struct InterpPosition(int line, int column)
{
    public static implicit operator LspPositition(InterpPosition interpPosition) => new()
    {
        Line = interpPosition.line,
        Character = interpPosition.column
    };
    public static implicit operator InterpPosition(LspPositition lspPosition) => new(lspPosition.Line, lspPosition.Character);
}

public record struct InterpScriptDiagnostics(string uri, InterpDiagnostic[] diagnostics)
{
    public static InterpScriptDiagnostics FromLsp(LspPublishDiagnosticsParams lspPublish) => new(
        lspPublish.Uri.ToString(),
        lspPublish.Diagnostics.Select(diagnostic => InterpDiagnostic.FromLsp(diagnostic)).ToArray()
    );
}

public record struct InterpDiagnostic(string message, InterpRange range, string severity)
{
    public static InterpDiagnostic FromLsp(LspDiagnostic lspDiagnostic) => new(
        lspDiagnostic.Message,
        lspDiagnostic.Range,
        lspDiagnostic.Severity.ToString()
    );
}