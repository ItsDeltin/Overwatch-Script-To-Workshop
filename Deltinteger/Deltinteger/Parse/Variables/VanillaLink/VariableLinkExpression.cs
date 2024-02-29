#nullable enable
using System.Linq;

namespace Deltin.Deltinteger.Parse.Variables.VanillaLink;

/// <summary>
/// Used by DataTypeAssigner to retrieve index references for ostw variables that target workshop variables.
/// </summary>
public interface IGetLinkedVariableAssigner
{
    /// <summary>
    /// Generates the link retriever from the current action set.
    /// </summary>
    LinkedVariableAssigner GetLinkRetriever(ActionSet actionSet);

    /// <summary>
    /// When the LinkedVariableAssigner is already known.
    /// </summary>
    public static IGetLinkedVariableAssigner? From(LinkedVariableAssigner assigner) =>
        assigner == null ? null : new FromValue(assigner);

    record FromValue(LinkedVariableAssigner Assigner) : IGetLinkedVariableAssigner
    {
        public LinkedVariableAssigner GetLinkRetriever(ActionSet actionSet) => Assigner;
    }
}

/// <summary>
/// A node for the vanilla targets in a variable declaration.
/// </summary>
public record class VariableLinkExpressionCollection(
    bool IsSpread,
    VariableLinkExpressionTarget[] Items
) : IGetLinkedVariableAssigner
{
    public LinkedVariableAssigner GetLinkRetriever(ActionSet actionSet) => new(
        actionSet,
        IsSpread,
        targetVariables: Items.Select(item => new TargetVariableStack(
            Name: item.LinkingTo,
            Indexer: item.Indexers.Select(i => i.Parse(actionSet)).ToArray())).ToArray()
    );
}

/// <summary>
/// One of {'a', 'b'[x], 'c'[..]}
/// </summary>
public readonly record struct VariableLinkExpressionTarget(string LinkingTo, IExpression[] Indexers);