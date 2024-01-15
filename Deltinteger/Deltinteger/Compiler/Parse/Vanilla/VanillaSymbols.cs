#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby2.Expand;
using SourceLobbySettings = Deltin.Deltinteger.Lobby2.Expand.LobbySettings;
namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

public readonly record struct VanillaSymbols(
    WorkshopSymbolTrie ScriptSymbols,
    WorkshopSymbolTrie LobbySettings,
    VanillaKeyword Actions,
    VanillaKeyword Conditions,
    VanillaKeyword Event,
    VanillaKeyword Variables,
    VanillaKeyword Subroutines,
    VanillaKeyword Settings
)
{
    public static readonly VanillaSymbols Instance = Load();

    public static VanillaSymbols Load()
    {
        var symbols = new WorkshopSymbolTrie();

        // Actions
        foreach (var action in ElementRoot.Instance.Actions)
            symbols.AddSymbol(action.Name, WorkshopLanguage.EnUS, new WorkshopItem.ActionValue(action));

        // Values
        foreach (var value in ElementRoot.Instance.Values)
            symbols.AddSymbol(value.Name, WorkshopLanguage.EnUS, new WorkshopItem.ActionValue(value));

        // Constants
        foreach (var enumerator in ElementRoot.Instance.Enumerators)
            foreach (var member in enumerator.Members)
                symbols.AddSymbol(member.WorkshopName(), WorkshopLanguage.EnUS, new WorkshopItem.Enumerator(member));

        // Settings
        var lobbySettings = new WorkshopSymbolTrie();
        foreach (var setting in IterAllWorkshopSettings(SourceLobbySettings.Instance!.Root))
        {
            lobbySettings.AddSymbol(setting.Name, WorkshopLanguage.EnUS, new WorkshopItem.LobbySetting(setting));
            foreach (var option in setting.Options)
            {
                lobbySettings.AddSymbol(option, WorkshopLanguage.EnUS, new WorkshopItem.LobbyValue(option));
            }
        }

        return new(
            ScriptSymbols: symbols,
            LobbySettings: lobbySettings,
            Actions: VanillaKeyword.EnKwForTesting("actions"),
            Conditions: VanillaKeyword.EnKwForTesting("conditions"),
            Event: VanillaKeyword.EnKwForTesting("event"),
            Variables: VanillaKeyword.EnKwForTesting("variables"),
            Subroutines: VanillaKeyword.EnKwForTesting("subroutines"),
            Settings: VanillaKeyword.EnKwForTesting("settings"));
    }

    static IEnumerable<EObject> IterAllWorkshopSettings(IEnumerable<EObject> settings)
    {
        foreach (var item in settings)
        {
            yield return item;
            foreach (var subitem in IterAllWorkshopSettings(item.Children))
                yield return subitem;
        }
    }
}

public readonly record struct VanillaKeyword(string EnUs, VanillaKeywordLanguageValue[] Translations)
{
    public bool Match(string text) => text == EnUs || Translations.Any(t => t.Value == text);

    public static VanillaKeyword EnKwForTesting(string value) => new(value, Array.Empty<VanillaKeywordLanguageValue>());
}
public readonly record struct VanillaKeywordLanguageValue(WorkshopLanguage Language, string Value);