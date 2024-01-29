#nullable enable

using System;
using System.Linq;

namespace Deltin.Deltinteger.Lobby2.Expand;

public class EObject
{
    public string Name { get; }
    public string Id { get; }
    public EObjectType Type { get; }
    public string[] Options { get; }
    public object? Default { get; }
    public EObject[] Children { get; set; } = Array.Empty<EObject>();

    public EObject(string name, string? id, EObjectType type, string[]? options, object? def)
    {
        Name = name;
        Id = id ?? name;
        Type = type;
        Options = options ?? type switch
        {
            EObjectType.OnOff => new[] { "On", "Off" },
            EObjectType.EnabledDisabled => new[] { "Enabled", "Disabled" },
            EObjectType.YesNo => new[] { "Yes", "No" },
            _ => Array.Empty<string>()
        };
        Default = def;
    }

    public EObject(string name, EObjectType type) : this(name, null, type, null, null) { }

    public bool HasContent() => Children.Length > 0;

    public string CompletionInsertText() => Type switch
    {
        EObjectType.Unknown or EObjectType.Switch => Name,
        EObjectType.Group => $"{Name} {{\n\t$0\n}}",
        EObjectType.Range => $"{Name}: ${{1:{Default ?? 100}}}%$0",
        EObjectType.Int => $"{Name}: ${{1:{Default ?? 0}}}$0",
        EObjectType.OnOff => $"{Name}: ${{1|On,Off|}}$0",
        EObjectType.YesNo => $"{Name}: ${{1|Yes,No|}}$0",
        EObjectType.EnabledDisabled => $"{Name}: ${{1|Enabled,Disabled|}}$0",
        EObjectType.String => $"{Name}: \"$1\"$0",
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
    YesNo,
    EnabledDisabled,
    Range,
    Int,
    Option,
    String
}

/// <summary>Assists in travelling through EObject descendants.</summary>
readonly struct SettingsTraveller
{
    public readonly EObject? CurrentObject;
    readonly EObject[]? currentChildren;

    private SettingsTraveller(EObject? currentObject, EObject[]? currentChildren)
    {
        CurrentObject = currentObject;
        this.currentChildren = currentChildren;
    }

    public SettingsTraveller Step(string name)
    {
        var next = currentChildren?.FirstOrDefault(c => c.Name == name);
        return new(next, next?.Children);
    }

    public static SettingsTraveller Root() => new(null, LobbySettings.Instance?.Root);
}