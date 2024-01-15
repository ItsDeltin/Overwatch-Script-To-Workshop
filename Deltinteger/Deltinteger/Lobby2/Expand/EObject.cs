#nullable enable

using System;

namespace Deltin.Deltinteger.Lobby2.Expand;

public class EObject
{
    public string Name { get; }
    public string Id { get; }
    public EObject[] Children { get; set; } = Array.Empty<EObject>();

    public EObject(string name, string? id)
    {
        Name = name;
        Id = id ?? name;
    }

    public bool HasContent() => Children.Length > 0;

    public override string ToString() => Name;
}