#nullable enable

using System.Collections.Generic;
namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

public class WorkshopSymbolTrie
{
    private readonly Dictionary<char, WorkshopSymbolTrie> children = new();
    private readonly HashSet<WorkshopLanguage> languages = new();
    public bool IsWord { get; private set; }

    public void AddSymbol(string value, WorkshopLanguage language)
    {
        languages.Add(language);

        if (string.IsNullOrEmpty(value))
        {
            IsWord = true;
        }
        else
        {
            GetOrAddPath(value[0]).AddSymbol(value[1..], language);
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
    string? lastValidWord = null;
    string currentWord = "";

    public WorkshopTrieTraveller(WorkshopSymbolTrie root) => current = root;

    public bool Next(char value)
    {
        currentWord += value;
        current?.TryGetTrie(value, out current);

        if (current is not null && current.IsWord)
        {
            lastValidWord = currentWord;
        }

        return current is null;
    }

    public readonly string? Word() => lastValidWord;
}