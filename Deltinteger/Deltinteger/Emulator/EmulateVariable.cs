#nullable enable

using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Emulator;

public class EmulateVariableSet
{
    readonly List<EmulateVariable> variables = [];

    public EmulateVariable GetVariable(string name)
    {
        if (!variables.TryGetValue(v => v.Name == name, out var variable))
        {
            variable = new(name, default);
            variables.Add(variable);
        }
        return variable;
    }
}

public class EmulateVariable(string name, EmulateValue? value)
{
    public string Name { get; } = name;
    public virtual EmulateValue Value { get; set; } = value ?? EmulateValue.Default;

    public void Modify(Func<EmulateValue, EmulateValue> modify)
    {
        Value = modify(Value);
    }

    public override string ToString() => $"{Name} = {Value}";
}

/// <summary>
/// This is used to represent an invalid variable, obtained with an invalid  player target
/// in a workshop element.
/// </summary>
sealed class FalseVariable(string name) : EmulateVariable(name, EmulateValue.Default)
{
    // Do not allow Value to actually be changed.
    public override EmulateValue Value { get => EmulateValue.Default; set { } }
}