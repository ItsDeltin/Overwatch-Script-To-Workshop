#nullable enable

using System;
using Deltin.Deltinteger.Elements;
namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

public readonly record struct VanillaSymbols(
    WorkshopSymbolTrie ActionValues,
    VanillaKeyword Actions,
    VanillaKeyword Conditions,
    VanillaKeyword Event,
    VanillaKeyword Variables
)
{
    public static readonly VanillaSymbols Instance = Load();

    public static VanillaSymbols Load()
    {
        var symbols = new WorkshopSymbolTrie();

        foreach (var action in ElementRoot.Instance.Actions)
            symbols.AddSymbol(action.Name, WorkshopLanguage.EnUS, new WorkshopItem.ActionValue(action));

        foreach (var value in ElementRoot.Instance.Values)
            symbols.AddSymbol(value.Name, WorkshopLanguage.EnUS, new WorkshopItem.ActionValue(value));

        foreach (var enumerator in ElementRoot.Instance.Enumerators)
            foreach (var member in enumerator.Members)
                symbols.AddSymbol(member.WorkshopName(), WorkshopLanguage.EnUS, new WorkshopItem.Enumerator(member));

        return new(symbols,
            Actions: EnKwForTesting("actions"),
            Conditions: EnKwForTesting("conditions"),
            Event: EnKwForTesting("event"),
            Variables: EnKwForTesting("variables"));
    }

    static VanillaKeyword EnKwForTesting(string value) => new(value, Array.Empty<VanillaKeywordLanguageValue>());
}

public readonly record struct VanillaKeyword(string EnUs, VanillaKeywordLanguageValue[] Translations);
public readonly record struct VanillaKeywordLanguageValue(WorkshopLanguage Language, string Value);