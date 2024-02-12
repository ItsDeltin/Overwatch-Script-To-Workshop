#nullable enable

using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse.Vanilla.Ide;

namespace Deltin.Deltinteger.Parse.Vanilla;

class VanillaContext
{
    public VanillaScope ScopedVariables { get; }
    public RuleEvent? EventType { get; init; }
    public BalancedActions? ActionBalancer { get; init; }
    readonly ScriptFile script;
    readonly IdeItems ideItems;
    ActiveParameterData activeParameterData;

    public VanillaContext(ScriptFile script, VanillaScope scopedVanillaVariables, IdeItems ideItems)
    {
        this.script = script;
        this.ideItems = ideItems;
        ScopedVariables = scopedVanillaVariables;
    }

    public VanillaContext(VanillaContext other)
    {
        ScopedVariables = other.ScopedVariables;
        ActionBalancer = other.ActionBalancer;
        script = other.script;
        ideItems = other.ideItems;
        EventType = other.EventType;
        activeParameterData = other.activeParameterData;
    }

    // Diagnostics
    public void Error(string text, DocRange range) => ideItems.Diagnostics.Add(new Diagnostic(text, range, Diagnostic.Error));
    public void Warning(string text, DocRange range) => ideItems.Diagnostics.Add(new Diagnostic(text, range, Diagnostic.Warning));
    public void Info(string text, DocRange range) => ideItems.Diagnostics.Add(new Diagnostic(text, range, Diagnostic.Information));
    public void Hint(string text, DocRange range) => ideItems.Diagnostics.Add(new Diagnostic(text, range, Diagnostic.Hint));

    // IDE
    public void AddCompletion(ICompletionRange completionRange) => ideItems.Completions.Add(completionRange);
    public void AddHover(DocRange range, MarkupBuilder content) => ideItems.Hovers.Add((range, content));
    public void AddSignatureInfo(ISignatureHelp signatureHelp) => ideItems.SignatureHelps.Add(signatureHelp);
    public void AddDocumentSymbol(DocumentSymbolNode symbol) => ideItems.DocumentSymbols.Add(symbol);

    // Context
    public ActiveParameterData GetActiveParameterData() => activeParameterData;
    public WorkshopLanguage[]? LikelyLanguages() => [];

    // Subcontext
    public VanillaContext SetEventType(RuleEvent? eventType) => new(this)
    {
        EventType = eventType
    };
    public VanillaContext SetActiveParameterData(ActiveParameterData data) => new(this)
    {
        activeParameterData = data
    };
    public VanillaContext AddActionBalancer(BalancedActions balancer) => new(this)
    {
        ActionBalancer = balancer
    };
    public VanillaContext ClearContext() => SetActiveParameterData(new());

    // Utility
    public Token? NextToken(Token previousToken) => script.NextToken(previousToken);

    // Types
    public VanillaType? VanillaTypeFromJsonName(string? name) => ElementJsonTypeHelper.FromString(StaticAnalysisData.Instance.TypeData, name);
}

readonly record struct ActiveParameterData(
    int? InvokeParameterCount = null,
    bool IsInvoked = false,
    bool NeedsStringLiteral = false,
    ExpectingVariable ExpectingVariable = default,
    bool ExpectingSubroutine = false,
    VanillaType? ExpectingType = default,
    bool ExpectingVariableIndexer = false
);

enum ExpectingVariable
{
    None,
    Global,
    Player
}

readonly struct IdeItems
{
    public readonly List<Diagnostic> Diagnostics = [];
    public readonly List<ICompletionRange> Completions = [];
    public readonly List<(DocRange, MarkupBuilder)> Hovers = [];
    public readonly List<ISignatureHelp> SignatureHelps = [];
    public readonly List<DocumentSymbolNode> DocumentSymbols = [];

    public IdeItems() { }

    public readonly void AddToScript(ScriptFile script)
    {
        foreach (var item in Diagnostics)
            script.Diagnostics.AddDiagnostic(item);

        foreach (var completion in Completions)
            script.AddCompletionRange(completion);

        foreach (var hover in Hovers)
            script.AddHover(hover.Item1, hover.Item2);

        foreach (var signature in SignatureHelps)
            script.AddSignatureInfo(signature);

        foreach (var symbol in DocumentSymbols)
            script.AddDocumentSymbol(symbol.ToLsp());
    }
}