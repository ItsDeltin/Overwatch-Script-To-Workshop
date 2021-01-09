using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathResolveClass : ClassType
    {
        /// <summary>An array of numbers where each value is that index's parent index. Following the path will lead to the source. Subtract value by -1 since 0 is used for unset.</summary>
        public ObjectVariable ParentArray { get; private set; }
        /// <summary>A reference to the source pathmap.</summary>
        public ObjectVariable Pathmap { get; private set; }
        /// <summary>A vector determining the destination.</summary>
        public ObjectVariable Destination { get; private set; }

        private readonly ITypeSupplier _supplier;

        public PathResolveClass(ITypeSupplier supplier) : base("PathResolve")
        {
            _supplier = supplier;
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            // Set ParentArray
            ParentArray = AddObjectVariable(new InternalVar("ParentArray"));

            // Set Pathmap
            Pathmap = AddObjectVariable(new InternalVar("OriginMap"));

            // Set Destination
            Destination = AddObjectVariable(new InternalVar("Destination"));

            serveObjectScope.AddNativeMethod(PathfindFunction);
            serveObjectScope.AddNativeMethod(Next);
        }

        private FuncMethod PathfindFunction => new FuncMethodBuilder()
        {
            Name = "Pathfind",
            Documentation = "Pathfinds the specified players to the destination.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players to pathfind.", _supplier.Players())
            },
            Action = (actionSet, call) =>
            {
                // Get the ResolveInfoComponent.
                ResolveInfoComponent resolveInfo = actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();

                // For each of the players, get the current.
                resolveInfo.Pathfind(actionSet, (Element)call.ParameterValues[0], Pathmap.Get(actionSet), ParentArray.Get(actionSet), Destination.Get(actionSet));

                return null;
            }
        };

        private FuncMethod Next => new FuncMethodBuilder()
        {
            Name = "Next",
            Documentation = new MarkupBuilder().Add("Gets a node's parent index from a node index. Continuously feeding the result back into this function will eventually lead to the source of the resolved path. The node's actual position can be obtained by calling ").Code("PathResolve.OriginMap.Nodes[node_index]").Add(".")
                .NewLine().Add("Identical to doing ").Code("parent_node_index = PathResolve.ParentArray[node_index] - 1"),
            Parameters = new CodeParameter[] {
                new CodeParameter("node", new MarkupBuilder().Add("The index of the node from the ").Code("PathResolve.OriginMap.Nodes").Add(" array."), _supplier.Number())
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) => ParentArray.Get(actionSet)[(Element)methodCall.ParameterValues[0]] - 1
        };
    }
}