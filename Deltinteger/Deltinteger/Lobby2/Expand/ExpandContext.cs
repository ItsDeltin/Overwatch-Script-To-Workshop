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
    readonly IList<EObject> repository;
    EObject? parent = default;

    public ExpandContext(
        IDictionary<string, Template> templates,
        IList<EObject> repository
        )
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

    public void Report(string message) { }

    public readonly Template? GetTemplate(string name) => templates.TryGetValue(name, out var template) ? template : null;

    public readonly bool TryGetRef(string id, [NotNullWhen(true)] out EObject? eObject)
    {
        return repository.TryGetValue(eObject => eObject.Id == id, out eObject);
    }
}