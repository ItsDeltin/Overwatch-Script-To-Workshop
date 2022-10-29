using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    public class BakemapClass : ISelfContainedClass
    {
        public string Name => "Bakemap";
        public SCClassProvider Provider { get; }
        public ClassType Instance => Provider.Instance;

        public ITypeVariable NodeBake { get; private set; }
        public ITypeVariable Pathmap { get; private set; }

        readonly DeltinScript _deltinScript;
        readonly PathfinderTypesComponent _pathfinderTypes;
        ITypeSupplier Types => _deltinScript.Types;

        public BakemapClass(DeltinScript deltinScript) : base()
        {
            _deltinScript = deltinScript;
            _pathfinderTypes = _deltinScript.GetComponent<PathfinderTypesComponent>();
            Provider = new SCClassProvider(deltinScript, this);
        }

        void ISelfContainedClass.Setup(SetupSelfContainedClass setup)
        {
            NodeBake = setup.AddObjectVariable(new InternalVar("NodeBake", Types.Any()));
            Pathmap = setup.AddObjectVariable(new InternalVar("Pathmap", _pathfinderTypes.Pathmap.Instance));
            setup.AddObjectMethod(Pathfind);
        }

        void ISelfContainedClass.New(ActionSet actionSet, NewClassInfo newClassInfo) { }
        MarkupBuilder ISelfContainedClass.Documentation => throw new System.NotImplementedException();

        private FuncMethodBuilder Pathfind => new FuncMethodBuilder()
        {
            Name = "Pathfind",
            Documentation = "Pathfinds specified players to the destination.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players to pathfind.", Types.Players()),
                new CodeParameter("destination", "The position to pathfind to.", Types.Vector())
            },
            Action = (actionSet, call) =>
            {
                // Get the ResolveInfoComponent.
                ResolveInfoComponent resolveInfo = actionSet.DeltinScript.GetComponent<ResolveInfoComponent>();

                // Get the Pathmap class.
                PathmapClass pathmapClass = actionSet.DeltinScript.GetComponent<PathfinderTypesComponent>().Pathmap;

                Element destination = call.Get(1);
                Element nodeArray = pathmapClass.Nodes.GetWithReference(actionSet.ToWorkshop, Pathmap.Get(actionSet));

                // Get the node closest to the destination.
                Element targetNode = PathfindUtility.GetBakemapTargetNode(nodeArray, destination);

                // For each of the players, get the current.
                resolveInfo.Pathfind(actionSet, call.Get(0), Pathmap.Get(actionSet), ValueInArray(NodeBake.Get(actionSet), targetNode), destination);
                return null;
            }
        };
    }
}