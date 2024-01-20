#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Deltin.Deltinteger.Lobby2.Json;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Lobby2.Expand;

struct ExpandContext
{
    public readonly EObject? Parent => parent;

    readonly IDictionary<string, Template> templates;
    readonly IList<SObject> repository;
    EObject? parent = default;
    FormatLinkedList? format = default;

    public ExpandContext(
        IDictionary<string, Template> templates,
        IList<SObject> repository)
    {
        this.templates = templates;
        this.repository = repository;
    }

    public readonly ExpandContext SetParent(EObject newParent)
    {
        var copy = this;
        copy.parent = newParent;
        return copy;
    }

    public readonly ExpandContext AddFormat(string key, string replaceWith)
    {
        ExpandContext copy = this;
        copy.format = new(copy.format, key, replaceWith);
        return copy;
    }

    public void Report(string message) { }

    public readonly Template? GetTemplate(string? name) =>
        name is null ? null : templates.TryGetValue(name, out var template) ? template : null;

    public readonly bool TryGetRef(string id, [NotNullWhen(true)] out SObject? eObject)
    {
        return repository.TryGetValue(eObject => eObject.Id == id, out eObject);
    }

    public readonly SObject? GetRef(string? id) => id is null ? null : repository.FirstOrDefault(obj => obj.Id == id);

    public readonly string FormatName(string inputName)
    {
        var current = format;
        while (current is not null)
        {
            inputName = inputName.Replace(current.Key, current.ReplaceWith);
            current = current.Parent;
        }
        return inputName;
    }
}

record FormatLinkedList(FormatLinkedList? Parent, string Key, string ReplaceWith);
