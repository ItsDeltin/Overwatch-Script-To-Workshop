#nullable enable
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Variables.VanillaLink;

/// <summary>
/// Provides gettable assigners with index references when targetting vanilla variables.
/// </summary>
public class LinkedVariableAssigner
{
    readonly ActionSet actionSet;
    readonly bool isSpread;
    readonly TargetVariableStack[] targetVariables;
    int current;

    public LinkedVariableAssigner(
        ActionSet actionSet,
        bool isSpread,
        TargetVariableStack[] targetVariables)
    {
        this.actionSet = actionSet;
        this.isSpread = isSpread;
        this.targetVariables = targetVariables;
    }

    public IndexReference Next()
    {
        var nextIndexReference = actionSet.LinkableVanillaVariables.GetVariable(
            targetVariables[current].Name,
            actionSet.IsGlobal
        )!;
        nextIndexReference = nextIndexReference.CreateChild(targetVariables[current].Indexer.Select(i => i as Element ?? Element.Num(0)).ToArray());

        current++;
        return nextIndexReference;
    }
}
public record struct TargetVariableStack(string Name, IWorkshopTree[] Indexer);