#nullable enable

using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Vanilla;

class VanillaContext
{
    public VanillaScope ScopedVariables { get; }
    public BalancedActions? ActionBalancer { get; init; }
    readonly ScriptFile script;
    ActiveParameterData activeParameterData;

    public VanillaContext(ScriptFile script, VanillaScope scopedVanillaVariables)
    {
        this.script = script;
        ScopedVariables = scopedVanillaVariables;
    }

    public VanillaContext(VanillaContext other)
    {
        ScopedVariables = other.ScopedVariables;
        ActionBalancer = other.ActionBalancer;
        script = other.script;
        activeParameterData = other.activeParameterData;
    }

    // Diagnostics
    public void Error(string text, DocRange range) => script.Diagnostics.Error(text, range);
    public void Warning(string text, DocRange range) => script.Diagnostics.Warning(text, range);
    public void Info(string text, DocRange range) => script.Diagnostics.Information(text, range);
    public void Hint(string text, DocRange range) => script.Diagnostics.Hint(text, range);

    // IDE
    public void AddCompletion(ICompletionRange completionRange) => script.AddCompletionRange(completionRange);
    public void AddHover(DocRange range, MarkupBuilder content) => script.AddHover(range, content);
    public void AddSignatureInfo(ISignatureHelp signatureHelp) => script.AddSignatureInfo(signatureHelp);

    // Context
    public ActiveParameterData GetActiveParameterData() => activeParameterData;
    public WorkshopLanguage[]? LikelyLanguages() => new WorkshopLanguage[0];

    // Subcontext
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
    public Token NextToken(Token previousToken) => script.NextToken(previousToken);

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