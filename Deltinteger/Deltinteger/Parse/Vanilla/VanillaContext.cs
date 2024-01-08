#nullable enable

using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltin.Deltinteger.Parse.Vanilla;

class VanillaContext
{
    readonly ScriptFile script;

    public VanillaContext(ScriptFile script)
    {
        this.script = script;
    }

    public void Error(string text, DocRange range) => script.Diagnostics.Error(text, range);
    public void Warning(string text, DocRange range) => script.Diagnostics.Warning(text, range);
    public void Info(string text, DocRange range) => script.Diagnostics.Information(text, range);
    public void Hint(string text, DocRange range) => script.Diagnostics.Hint(text, range);

    public int? InvokeParameterCount() => 0;

    public ActiveParameterData? GetActiveParameterData() => new();

    public VanillaContext ExpectingType(VanillaType type) => this; // todo

    public WorkshopLanguage[]? LikelyLanguages() => new WorkshopLanguage[0];
}

readonly record struct ActiveParameterData(int ParameterCount);