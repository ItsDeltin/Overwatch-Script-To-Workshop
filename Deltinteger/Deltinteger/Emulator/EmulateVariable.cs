#nullable enable

using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Emulator;

class EmulateVariableSet
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
    public EmulateValue Value { get; set; } = value ?? EmulateValue.Default;

    public void Modify(Func<EmulateValue, EmulateValue> modify)
    {
        Value = modify(Value);
    }

    public override string ToString() => $"{Name} = {Value}";
}