#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse.Vanilla;

class BalancedActions
{
    readonly Stack<BalancedItem> balanceStack = new();
    readonly List<BalanceRange> ranges = new();
    DocPos? pos;

    public void SetCurrentPosition(DocPos pos)
    {
        this.pos = pos;
    }

    public BalancedItem Current()
    {
        balanceStack.TryPeek(out var current);
        return current;
    }

    public void Simple()
    {
        Push(BalancedItem.Simple);
    }

    public void If()
    {
        Push(BalancedItem.IfChain);
    }

    public void ElseIf()
    {
        if (Current() == BalancedItem.IfChain)
        {
            balanceStack.Pop();
        }
        Push(BalancedItem.IfChain);
    }

    public void End()
    {
        AddRange(BalancedItem.None);
        balanceStack.TryPop(out _);
    }

    void AddRange(BalancedItem type)
    {
        if (pos is not null && (!balanceStack.TryPeek(out var top) || top != type))
        {
            ranges.Add(new(pos, type));
        }
    }

    void Push(BalancedItem type)
    {
        AddRange(type);
        balanceStack.Push(type);
    }

    public void FromFunction(string name)
    {
        switch (name)
        {
            case "End":
                End();
                break;

            case "If":
                If();
                break;

            case "Else If":
                ElseIf();
                break;

            case "For Global Variable":
            case "For Player Variable":
            case "While":
            case "Else":
                Simple();
                break;
        }
    }

    public string[] GetNotableActionsFromPos(DocPos pos) =>
        ranges.LastOrDefault(r => r.Start < pos)?.GetNotableFunctions() ?? Array.Empty<string>();
}

enum BalancedItem
{
    None,
    IfChain,
    Simple
}

record BalanceRange(DocPos Start, BalancedItem Type)
{
    public string[] GetNotableFunctions() => Type switch
    {
        BalancedItem.IfChain => NotableIfChain,
        BalancedItem.Simple => NotableOther,
        BalancedItem.None or _ => Array.Empty<string>(),
    };

    static readonly string[] NotableIfChain = new string[] { "Else", "Else If", "End" };
    static readonly string[] NotableOther = new string[] { "End" };
}