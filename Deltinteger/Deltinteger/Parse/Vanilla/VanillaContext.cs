#nullable enable

using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltin.Deltinteger.Parse.Vanilla;

class VanillaContext
{
    readonly ScriptFile script;
    ActiveParameterData activeParameterData;
    VanillaType expectedType;

    public VanillaContext(ScriptFile script)
    {
        this.script = script;
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
    public int? InvokeParameterCount() => 0;
    public ActiveParameterData GetActiveParameterData() => activeParameterData;
    public WorkshopLanguage[]? LikelyLanguages() => new WorkshopLanguage[0];
    public VanillaType GetExpectedType() => expectedType;

    // Subcontext
    public VanillaContext SetActiveParameterData(ActiveParameterData data) => new(script)
    {
        activeParameterData = data
    };
    public VanillaContext ExpectingType(VanillaType type) => this; // todo
}

readonly record struct ActiveParameterData(
    bool IsInvoked = false,
    bool NeedsStringLiteral = false
);