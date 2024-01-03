#nullable enable

using Deltin.Deltinteger.Elements;
namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

public readonly record struct VanillaSymbols(WorkshopSymbolTrie ActionValues)
{
    public static readonly VanillaSymbols Instance = Load();

    public static VanillaSymbols Load()
    {
        var symbols = new WorkshopSymbolTrie();

        foreach (var action in ElementRoot.Instance.Actions)
            symbols.AddSymbol(action.Name, WorkshopLanguage.EnUS);

        foreach (var value in ElementRoot.Instance.Values)
            symbols.AddSymbol(value.Name, WorkshopLanguage.EnUS);

        foreach (var enumerator in ElementRoot.Instance.Enumerators)
            foreach (var member in enumerator.Members)
                symbols.AddSymbol(member.WorkshopName(), WorkshopLanguage.EnUS);

        return new(symbols);
    }
}