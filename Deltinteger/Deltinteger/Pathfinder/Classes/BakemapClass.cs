using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    public class BakemapClass : ISelfContainedClass
    {
        public string Name => "Bakemap";
        public SelfContainedClassInstance Instance { get; }

        public ObjectVariable NodeBake { get; private set; }
        public ObjectVariable Pathmap { get; private set; }
        private readonly ITypeSupplier _types;

        public BakemapClass(DeltinScript deltinScript) : base()
        {
            _types = deltinScript.Types;
            Instance = new SelfContainedClassInstance(deltinScript, this);
        }

        void ISelfContainedClass.Setup(SetupSelfContainedClass setup)
        {
            var nodeBake = new InternalVar("NodeBake");
            var pathmap = new InternalVar("Pathmap");

            setup.AddObjectVariable(nodeBake);
            setup.AddObjectVariable(pathmap);
            setup.ObjectScope.AddNativeMethod(Pathfind);

            NodeBake = new ObjectVariable(Instance, nodeBake);
            Pathmap = new ObjectVariable(Instance, pathmap);
        }
        void ISelfContainedClass.AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner) {}
        void ISelfContainedClass.New(ActionSet actionSet, NewClassInfo newClassInfo) {}
        MarkupBuilder ISelfContainedClass.Documentation => throw new System.NotImplementedException();
        Constructor[] ISelfContainedClass.Constructors => new Constructor[0];

        private FuncMethod Pathfind => new FuncMethodBuilder() {
            Name = "Pathfind",
            Documentation = "Pathfinds specified players to the destination.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players to pathfind.", _types.Players()),
                new CodeParameter("destination", "The position to pathfind to.", _types.Vector())
            },
            Action = (actionSet, call) =>
            {
                // Get the ResolveInfoComponent.
                ResolveInfoComponent resolveInfo = actionSet.DeltinScript.GetComponent<ResolveInfoComponent>();

                // Get the Pathmap class.
                PathmapClass pathmapClass = actionSet.DeltinScript.GetComponent<PathfinderTypesComponent>().Pathmap;

                Element destination = call.Get(1);
                Element nodeArray = pathmapClass.Nodes.Get()[Pathmap.Get(actionSet)];

                // Get the node closest to the destination.
                Element targetNode = IndexOfArrayValue(
                    nodeArray,
                    FirstOf(Sort(
                        // Sort non-null nodes
                        /*Element.Part<V_FilteredArray>(nodeArray, new V_ArrayElement()),*/
                        nodeArray,
                        // Sort by distance to destination
                        DistanceBetween(ArrayElement(), destination)
                    ))
                );

                // For each of the players, get the current.
                resolveInfo.Pathfind(actionSet, call.Get(0), (Element)Pathmap.Get(actionSet), ValueInArray(NodeBake.Get(actionSet), targetNode), destination);
                return null;
            }
        };
    }
}