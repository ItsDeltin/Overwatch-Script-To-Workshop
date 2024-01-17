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
        Options = options ?? type switch
        {
            EObjectType.OnOff => new[] { "On", "Off" },
            EObjectType.EnabledDisabled => new[] { "Enabled", "Disabled" },
            _ => Array.Empty<string>()
        };
    }

    public EObject(string name, EObjectType type) : this(name, null, type, null) { }

    public bool HasContent() => Children.Length > 0;

    public string CompletionInsertText() => Type switch
    {
        EObjectType.Unknown or EObjectType.Switch => Name,
        EObjectType.Group => $"{Name} {{\n\t$0\n}}",
        EObjectType.Range => $"{Name}: ${{1:100}}%$0",
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