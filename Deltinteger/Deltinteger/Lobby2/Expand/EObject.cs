#nullable enable

using System;

namespace Deltin.Deltinteger.Lobby2.Expand;

public class EObject
{
    public string Name { get; }
    public string Id { get; }
    public EObjectType Type { get; }
    public string[] Options { get; }
    public EObject[] Children { get; set; } = Array.Empty<EObject>();

    public EObject(string name, string? id, EObjectType type, string[]? options)
    {
        Name = name;
        Id = id ?? name;
        Type = type;
        Options = options ?? Array.Empty<string>();
    }

    public bool HasContent() => Children.Length > 0;

    public string CompletionInsertText() => Type switch
    {
        EObjectType.Unknown or EObjectType.Group or EObjectType.Switch => Name,
        _ => $"{Name}: "
    };

    public override string ToString() => Name;
}

public enum EObjectType
{
    Unknown,
    Any,
    Group,
    Switch,
    OnOff,
    EnabledDisabled,
    Range,
    Int,
    Option
}