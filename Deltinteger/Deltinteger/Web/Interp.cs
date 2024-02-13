namespace Deltin.Deltinteger.LanguageServer.Model;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspPositition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LspDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LspPublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;
using LspCompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using LspSignatureHelp = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelp;
using LspSignature = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureInformation;
using LspSignatureParameter = OmniSharp.Extensions.LanguageServer.Protocol.Models.ParameterInformation;
using LspSignatureContext = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelpContext;
using LspSignatureHelpTriggerKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelpTriggerKind;
using LspDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using LspSemanticTokens = OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokens;
using LspHover = OmniSharp.Extensions.LanguageServer.Protocol.Models.Hover;
using System;
using System.Linq;
using Web;

// This file recreates some of the LSP protocol as structs so
// that they can be used with JsExport

#nullable enable
#pragma warning disable IDE1006

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

public record struct InterpPosition(int line, int character)
{
    public static implicit operator LspPositition(InterpPosition interpPosition) => new()
    {
        Line = interpPosition.line,
        Character = interpPosition.character
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
        (lspDiagnostic.Severity ?? LspDiagnosticSeverity.Error).ToString()
    );
}

public record struct InterpCompletionItem(string label, string kind, string detail, string documentationMarkdown, string insertText, string filterText, string sortText, string documentation)
{
    public static InterpCompletionItem FromLsp(LspCompletionItem lspCompletionItem) => new(
        lspCompletionItem.Label,
        lspCompletionItem.Kind.ToString(),
        lspCompletionItem.Detail ?? "",
        LspUtility.GetMarkdownContent(lspCompletionItem.Documentation),
        lspCompletionItem.InsertText ?? lspCompletionItem.Label,
        lspCompletionItem.FilterText ?? lspCompletionItem.Label,
        lspCompletionItem.SortText ?? lspCompletionItem.Label,
        LspUtility.GetMarkdownContent(lspCompletionItem.Documentation)
    );
}

public record struct InterpSignatureHelp(int? activeParameter, int? activeSignature, InterpSignature[] signatures)
{
    public static InterpSignatureHelp FromLsp(LspSignatureHelp lspSignatureHelp) => new(lspSignatureHelp.ActiveParameter, lspSignatureHelp.ActiveSignature, lspSignatureHelp.Signatures.Select(s => InterpSignature.FromLsp(s)).ToArray());

    public readonly LspSignatureHelp ToLsp() => new()
    {
        ActiveParameter = activeParameter,
        ActiveSignature = activeSignature,
        Signatures = signatures?.Select(p => p.ToLsp()).ToArray() ?? Array.Empty<LspSignature>()
    };
}

public record struct InterpSignature(string label, InterpSignatureParameter[] parameters, int? activeParameter, string documentation)
{
    public static InterpSignature FromLsp(LspSignature lspSignature) => new(lspSignature.Label, lspSignature.Parameters?.Select(p => InterpSignatureParameter.FromLsp(p)).ToArray() ?? Array.Empty<InterpSignatureParameter>(), lspSignature.ActiveParameter, LspUtility.GetMarkdownContent(lspSignature.Documentation));

    public readonly LspSignature ToLsp() => new()
    {
        ActiveParameter = activeParameter,
        Documentation = documentation,
        Label = label,
        Parameters = parameters.Select(p => p.ToLsp()).ToArray()
    };
}

public record struct InterpSignatureParameter(string label, string documentation)
{
    public static InterpSignatureParameter FromLsp(LspSignatureParameter lspSignatureParameter) => new(
        lspSignatureParameter.Label.ToString(),
        LspUtility.GetMarkdownContent(lspSignatureParameter.Documentation));

    public readonly LspSignatureParameter ToLsp() => new()
    {
        Documentation = documentation,
        Label = label
    };
}

public record struct InterpSignatureContext(string signatureHelpTriggerKind, string? triggerCharacter, bool isRetrigger, InterpSignatureHelp? activeSignatureHelp)
{
    public readonly LspSignatureContext ToLsp() => new()
    {
        ActiveSignatureHelp = activeSignatureHelp?.ToLsp(),
        IsRetrigger = isRetrigger,
        TriggerCharacter = triggerCharacter,
        TriggerKind = signatureHelpTriggerKind switch
        {
            "TriggerCharacter" => LspSignatureHelpTriggerKind.TriggerCharacter,
            "ContentChange" => LspSignatureHelpTriggerKind.ContentChange,
            "Invoke" or _ => LspSignatureHelpTriggerKind.Invoked,
        }
    };
}

public record struct InterpSemantics(uint[] data, InterpSemanticsEdit[] edits, string? resultId)
{
    public static InterpSemantics FromLsp(LspSemanticTokens? semanticTokens) => new(
        semanticTokens?.Data.Select(d => (uint)d).ToArray() ?? Array.Empty<uint>(),
        Array.Empty<InterpSemanticsEdit>(),
        null
    );
}

public record struct InterpSemanticsEdit(int deleteCount, int start, uint[]? data);

public record struct InterpHover(string[] contents, InterpRange range)
{
    public static InterpHover FromLsp(LspHover hover) => new(
        LspUtility.GetMarkdownContent(hover.Contents),
        hover.Range ?? new InterpRange()
    );
}