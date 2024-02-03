#nullable enable

using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby2.Expand;
namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

public class WorkshopSymbolTrie
{
    private readonly Dictionary<char, WorkshopSymbolTrie> children = new();
    private readonly HashSet<WorkshopLanguage> languages = new();
    private readonly HashSet<LanguageLinkedWorkshopItem> workshopItems = new();

    public bool IsWord() => workshopItems.Count != 0;

    public IReadOnlySet<LanguageLinkedWorkshopItem> GetItems() => workshopItems;

    public void AddSymbol(string value, WorkshopLanguage language, WorkshopItem item)
    {
        AddSymbolInternal(value.ToLower(), language, item);
    }

    void AddSymbolInternal(string value, WorkshopLanguage language, WorkshopItem item)
    {
        languages.Add(language);

        if (string.IsNullOrEmpty(value))
        {
            workshopItems.Add(new(language, item));
        }
        else
        {
            GetOrAddPath(value[0]).AddSymbolInternal(value[1..], language, item);
        }
    }

    WorkshopSymbolTrie GetOrAddPath(char character)
    {
        if (!children.TryGetValue(character, out WorkshopSymbolTrie? symbolTrie))
        {
            symbolTrie = new WorkshopSymbolTrie();
            children.Add(character, symbolTrie);
        }
        return symbolTrie;
    }

    public bool TryGetTrie(char character, out WorkshopSymbolTrie? trie) => children.TryGetValue(character, out trie);

    public WorkshopTrieTraveller Travel() => new(this);
}

public struct WorkshopTrieTraveller
{
    WorkshopSymbolTrie? current;
    (string Word, WorkshopSymbolTrie Trie)? lastValid = null;
    string currentWord = "";

    public WorkshopTrieTraveller(WorkshopSymbolTrie root) => current = root;

    public bool Next(char value)
    {
        currentWord += value;
        current?.TryGetTrie(value, out current);

        if (current is not null && current.IsWord())
        {
            lastValid = (currentWord, current);
        }

        return current is not null;
    }

    public readonly (string Word, IReadOnlySet<LanguageLinkedWorkshopItem> LinkedItems)? Word() =>
        lastValid.HasValue ? new(lastValid.Value.Word, lastValid.Value.Trie.GetItems()) : null;
}

public abstract record WorkshopItem
{
    public record ActionValue(ElementBaseJson Value) : WorkshopItem;
    public record Enumerator(ElementEnumMember Member) : WorkshopItem;
    public record LobbySetting(EObject Setting) : WorkshopItem;
    public record LobbyValue(string EnUs) : WorkshopItem;
}

public record struct LanguageLinkedWorkshopItem(WorkshopLanguage Language, WorkshopItem Item);