using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using System;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathResolveClass : ISelfContainedClass
    {
        public string Name => "PathResolve";
        public SelfContainedClassProvider Provider { get; }
        public SelfContainedClassInstance Instance => Provider.Instance;

        /// <summary>An array of numbers where each value is that index's parent index. Following the path will lead to the source. Subtract value by -1 since 0 is used for unset.</summary>
        public ObjectVariable ParentArray { get; private set; }
        /// <summary>A reference to the source pathmap.</summary>
        public ObjectVariable Pathmap { get; private set; }
        /// <summary>A vector determining the destination.</summary>
        public ObjectVariable Destination { get; private set; }

        readonly ITypeSupplier _supplier;
        readonly PathfinderTypesComponent _pathfinderTypes;

        public PathResolveClass(DeltinScript deltinScript)
        {
            _supplier = deltinScript.Types;
            _pathfinderTypes = deltinScript.GetComponent<PathfinderTypesComponent>();
            Provider = new SelfContainedClassProvider(deltinScript, this);
        }

        public void Setup(SetupSelfContainedClass setup)
        {
            ParentArray = setup.AddObjectVariable(new InternalVar("ParentArray", _supplier.Any()));
            Pathmap = setup.AddObjectVariable(new InternalVar("OriginMap", _pathfinderTypes.Pathmap.Instance));
            Destination = setup.AddObjectVariable(new InternalVar("Destination", _supplier.Vector()));

            setup.ObjectScope.AddNativeMethod(PathfindFunction);
            setup.ObjectScope.AddNativeMethod(Next);
        }

        public void WorkshopInit(DeltinScript translateInfo) => throw new NotImplementedException();
        public void New(ActionSet actionSet, NewClassInfo newClassInfo) => throw new NotImplementedException();

        private FuncMethod PathfindFunction => new FuncMethodBuilder() {
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
                resolveInfo.Pathfind(actionSet, (Element)call.ParameterValues[0], (Element)Pathmap.Get(actionSet), (Element)ParentArray.Get(actionSet), (Element)Destination.Get(actionSet));

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
            Action = (actionSet, methodCall) => Element.ValueInArray(ParentArray.Get(actionSet), (Element)methodCall.ParameterValues[0]) - 1
        };

        public MarkupBuilder Documentation => new MarkupBuilder(null);
        public Constructor[] Constructors => new Constructor[0];
    }
}