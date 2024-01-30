#nullable enable
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Lobby2.Expand;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Lobby2.KeyValues;

public class SettingKeyValue
{
    public Variant<EObject, string> Name { get; set; }
    public ISettingValue? Value { get; set; }
    public bool Disabled { get; set; }

    public SettingKeyValue(Variant<EObject, string> name, ISettingValue? value)
    {
        Name = name;
        Value = value;
    }

    public string Symbol() => Name.Match(eObject => eObject.Name, name => name);

    public bool Conflicts(SettingKeyValue other)
    {
        return Name.Match(a => other.Name.Match(
            b => a.Name == b.Name,
            b => false
        ), a => other.Name.Match(
            b => false,
            b => a == b
        ));
    }
}

public interface ISettingValue
{
    ISettingValue Merge(ISettingValue other);

    void ToWorkshop(WorkshopBuilder builder);
}

public class GroupSettingValue : ISettingValue
{
    readonly List<SettingKeyValue> keyValues;

    public GroupSettingValue(List<SettingKeyValue> keyValues) => this.keyValues = keyValues;
    public GroupSettingValue() => keyValues = new();

    public ISettingValue Merge(ISettingValue other)
    {
        if (other is not GroupSettingValue groupValue)
            return other;

        foreach (var add in groupValue.keyValues)
            Add(add);

        return this;
    }

    public void ToWorkshopTop(WorkshopBuilder builder)
    {
        if (keyValues.Count == 0)
            return;

        builder.AppendKeyword("settings");
        ToWorkshop(builder);
    }

    public void ToWorkshop(WorkshopBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("{");
        builder.Indent();
        foreach (var keyValue in keyValues)
        {
            if (keyValue.Disabled)
                builder.AppendKeyword("disabled").Append(" ");

            builder.Append(keyValue.Symbol());
            if (keyValue.Value is not null)
            {
                keyValue.Value.ToWorkshop(builder);
            }
            else
            {
                builder.AppendLine();
            }
        }
        builder.Outdent();
        builder.AppendLine("}");
    }

    public void Add(SettingKeyValue add)
    {
        int replaceIndex = keyValues.FindIndex(kv => kv.Conflicts(add));
        // Key being added does not exist.
        if (replaceIndex == -1)
            keyValues.Add(add);
        // If both settings are groups, merge them.
        else if (keyValues[replaceIndex].Value is GroupSettingValue thisKeyGroup
            && add.Value is GroupSettingValue otherKeyGroup)
        {
            thisKeyGroup.Merge(otherKeyGroup);
        }
        // Replace key
        else
            keyValues[replaceIndex].Value = add.Value;
    }

    public SettingKeyValue? Get(Variant<EObject, string> name)
    {
        return keyValues.FirstOrDefault(kv => kv.Name == name);
    }
}

record NumberSettingValue(double Value, bool Percentage) : ISettingValue
{
    public ISettingValue Merge(ISettingValue Other) => Other;

    public void ToWorkshop(WorkshopBuilder builder)
    {
        builder.AppendLine($": {Value}{(Percentage ? "%" : "")}");
    }
}

record OptionSettingValue(string Text) : ISettingValue
{
    public ISettingValue Merge(ISettingValue Other) => Other;

    public void ToWorkshop(WorkshopBuilder builder)
    {
        builder.AppendLine($": {Text}");
    }
}

record StringSettingValue(string Text) : ISettingValue
{
    public ISettingValue Merge(ISettingValue Other) => Other;

    public void ToWorkshop(WorkshopBuilder builder)
    {
        builder.AppendLine($": \"{Text}\"");
    }
}