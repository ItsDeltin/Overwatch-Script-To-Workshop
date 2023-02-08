using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder.Walker
{
    /// <summary>
    /// Contains the data required to execute a pathfind for one or more players to a single destination.
    /// </summary>
    /// <param name="ActionSet">The rule's ActionSet where the pathfind will be executed.</param>
    /// <param name="Players">The players that are pathfinding.</param>
    /// <param name="Bake">The pathmap's baked data.</param>
    /// <param name="Nodes">The nodes in the pathmap.</param>
    /// <param name="Destination">The pathfinding destination.</param>
    record struct ExecutorArgs(ActionSet ActionSet, Element Players, Element Bake, Element Nodes, Element Destination);
}