#nullable enable
using System.Collections.Generic;
using Deltin.Deltinteger.Lobby2.Expand;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Lobby2.KeyValues;

class SettingKeyValue
{
    public Variant<EObject, string> Name { get; set; }
    public ISettingValue? Value { get; set; }

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

interface ISettingValue
{
    ISettingValue Merge(ISettingValue other);

    void ToWorkshop(WorkshopBuilder builder);
}

class GroupSettingValue : ISettingValue
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
        if (replaceIndex == -1)
            keyValues.Add(add);
        else
            keyValues[replaceIndex].Value = add.Value;
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