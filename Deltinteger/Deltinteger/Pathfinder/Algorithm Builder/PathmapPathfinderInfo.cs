using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    class SharedPathfinderInfoValues
    {
        public ActionSet ActionSet { get; set; }
        public Element PathmapObject { get; set; }
        public Element Attributes { get; set; }
        public ILambdaInvocable OnLoop { get; set; }
        public ILambdaInvocable OnConnectLoop { get; set; }
        public INodeFromPosition NodeFromPosition { get; set; }
    }

    abstract class PathmapPathfinderInfo : IPathfinderInfo
    {
        public ActionSet ActionSet { get; }
        public Element InitialNode { get; }
        public Element EnabledAttributes { get; }

        private readonly ILambdaInvocable _onLoop;
        private readonly ILambdaInvocable _onConnectLoop;
        private readonly PathmapClass _pathmapClass;
        private readonly INodeFromPosition _nodeFromPosition;

        protected Element OriginalPosition { get; }
        protected Element PathmapObject { get; }
        protected ResolveInfoComponent ResolveInfo { get; }

        protected PathfindAlgorithmBuilder Builder { get; private set; }

        public PathmapPathfinderInfo(Element position, SharedPathfinderInfoValues pathfinderValues)
        {
            ActionSet = pathfinderValues.ActionSet;
            InitialNode = pathfinderValues.NodeFromPosition.NodeFromPosition(position);
            EnabledAttributes = pathfinderValues.Attributes;
            _onLoop = pathfinderValues.OnLoop;
            _onConnectLoop = pathfinderValues.OnConnectLoop;
            _pathmapClass = ActionSet.DeltinScript.GetComponent<PathfinderTypesComponent>().Pathmap;
            _nodeFromPosition = pathfinderValues.NodeFromPosition;
            OriginalPosition = position;
            PathmapObject = pathfinderValues.PathmapObject;
            ResolveInfo = ActionSet.DeltinScript.GetComponent<ResolveInfoComponent>();
        }

        public void Run()
        {
            Init();
            Builder = new PathfindAlgorithmBuilder(this);
            Builder.Get();
        }

        public void OnConnectLoop()
        {
            if (_onConnectLoop == null)
                ActionSet.AddAction(Wait());
            else
                _onConnectLoop.Invoke(ActionSet);
        }

        public void OnLoop()
        {
            if (_onLoop == null)
                ActionSet.AddAction(Wait());
            else
                _onLoop.Invoke(ActionSet);
        }

        protected virtual void Init() {}
        public abstract void OnLoopEnd();
        public abstract void Finished();

        public abstract Element LoopCondition { get; }
        public Element NodeArray => _pathmapClass.Nodes.Get(ActionSet.ToWorkshop, PathmapObject);
        public Element SegmentArray => _pathmapClass.Segments.Get(ActionSet.ToWorkshop, PathmapObject);
        public Element AttributeArray => _pathmapClass.Attributes.Get(ActionSet.ToWorkshop, PathmapObject);

        protected Element NodeFromPosition(Element position) => _nodeFromPosition.NodeFromPosition(position);
    }

    /// <summary>Pathfinds a single player to a destination.</summary>
    class PathfindPlayer : PathmapPathfinderInfo
    {
        private readonly Element _player;
        private readonly Element _destination;
        private SkipStartMarker _playerNodeReachedBreak;

        public PathfindPlayer(Element player, Element destination, SharedPathfinderInfoValues pathfinderInfo) : base(destination, pathfinderInfo)
        {
            _player = player;
            _destination = destination;
        }

        public override Element LoopCondition => Builder.AnyAccessableUnvisited();

        public override void OnLoopEnd()
        {
            // Break out of the while loop when the current node is the closest node to the player.
            _playerNodeReachedBreak = new SkipStartMarker(ActionSet, Compare(
                NodeFromPosition(PositionOf(_player)),
                Operator.NotEqual,
                Builder.Current.GetVariable()
            ));
            ActionSet.AddAction(_playerNodeReachedBreak);
        }

        public override void Finished()
        {
            SkipEndMarker endLoop = new SkipEndMarker();
            ActionSet.AddAction(endLoop);
            _playerNodeReachedBreak.SetEndMarker(endLoop);

            ResolveInfo.Pathfind(ActionSet, _player, PathmapObject, Builder.ParentArray.Get(), _destination);
        }
    }

    /// <summary>Pathfinds multiple players to a destination.</summary>
    class PathfindAll : PathmapPathfinderInfo
    {
        private readonly Element _players;
        private readonly Element _destination;
        private IndexReference _closestNodesToPlayers;

        public PathfindAll(Element players, Element destination, SharedPathfinderInfoValues pathfinderInfo) : base(destination, pathfinderInfo)
        {
            _players = players;
            _destination = destination;
        }

        protected override void Init()
        {
            // Assign an array that will be used to store the closest node to each player.
            _closestNodesToPlayers = ActionSet.VarCollection.Assign("Dijkstra: Closest nodes", ActionSet.IsGlobal, false);
            ActionSet.AddAction(_closestNodesToPlayers.SetVariable(EmptyArray()));

            // Loop through each player and get the closest node.
            ForeachBuilder getClosestNodes = new ForeachBuilder(ActionSet, _players);
            ActionSet.AddAction(_closestNodesToPlayers.ModifyVariable(Operation.AppendToArray, NodeFromPosition(getClosestNodes.IndexValue)));
            getClosestNodes.Finish();
        }

        public override Element LoopCondition => Any(
            _closestNodesToPlayers.GetVariable(),
            Contains(
                Builder.Unvisited.GetVariable(),
                ArrayElement()
            )
        );

        public override void OnLoopEnd() {}

        public override void Finished()
        {
            ResolveInfo.Pathfind(ActionSet, _players, PathmapObject, Builder.ParentArray.Get(), _destination);
        }
    }

    /// <summary>Creates a PathResolve.</summary>
    class ResolvePathfind : PathmapPathfinderInfo
    {
        private readonly Element _destination;
        private readonly PathResolveClass _pathResolveClass;
        private IndexReference _sourceNode;
        private IndexReference _classReference; // The created PathResolve reference.

        public Element Result => _classReference.Get();

        public ResolvePathfind(Element position, SharedPathfinderInfoValues pathfinderInfo) : base(position, pathfinderInfo)
        {
            var pathfinderTypes = ActionSet.DeltinScript.GetComponent<PathfinderTypesComponent>();
            _pathResolveClass = pathfinderTypes.PathResolve;
        }
        public ResolvePathfind(Element position, Element destination, SharedPathfinderInfoValues pathfinderInfo) : this(position, pathfinderInfo) => _destination = destination;

        public override Element LoopCondition { get {
            if (_destination == null)
                // return Element.Part<V_CountOf>(Builder.Unvisited.GetVariable()) > 0;
                return Builder.AnyAccessableUnvisited();
            else
                return Contains(
                    Builder.Unvisited.GetVariable(),
                    _sourceNode.GetVariable()
                );
        }}

        protected override void Init()
        {
            // Get the PathResolveClass instance.
            _pathResolveClass.WorkshopInit(ActionSet.Translate.DeltinScript);

            // Create a new PathResolve class instance.
            _classReference = _pathResolveClass.Instance.Create(ActionSet, ActionSet.Translate.DeltinScript.GetComponent<ClassData>());

            // Save the pathmap.
            _pathResolveClass.Pathmap.SetWithReference(ActionSet, _classReference.Get(), (Element)ActionSet.CurrentObject);

            // Save the destination.
            _pathResolveClass.Destination.SetWithReference(ActionSet, _classReference.Get(), OriginalPosition);

            // Assign FinalNode
            if (_destination != null)
            {
                _sourceNode = ActionSet.VarCollection.Assign("Final Node", ActionSet.IsGlobal, PathfindAlgorithmBuilder.AssignExtended);
                _sourceNode.SetVariable(NodeFromPosition(_destination));
            }
        }

        public override void OnLoopEnd()
        {
            ActionSet.AddAction(If(Builder.NoAccessableUnvisited()));
            ActionSet.AddAction(Break());
            ActionSet.AddAction(End());
        }

        public override void Finished()
        {
            // Save parent arrays.
            _pathResolveClass.ParentArray.SetWithReference(ActionSet, _classReference.Get(), Builder.ParentArray.Get());
        }
    }

    /// <summary>Pathfinds a player to the closest destination in an array.</summary>
    class PathfindEither : PathmapPathfinderInfo
    {
        private readonly Element _player; // The player that is pathfinding.
        private readonly Element _destinations; // The potential destinations.
        private IndexReference _potentialDestinationNodes; // The nodes of the potential destinations.
        private IndexReference _chosenDestination; // The chosen destination node.

        public PathfindEither(Element player, Element destinations, SharedPathfinderInfoValues pathfinderValues)
            : base(PositionOf(player), pathfinderValues)
        {
            _player = player;
            _destinations = destinations;
        }

        // Loop until any of the destinations have been visited.
        public override Element LoopCondition => All(
            _potentialDestinationNodes.Get(),
            Element.Contains(
                Builder.Unvisited.Get(),
                ArrayElement()
            )
        );

        protected override void Init()
        {
            _chosenDestination = ActionSet.VarCollection.Assign("Dijkstra: Chosen Destination", ActionSet.IsGlobal, PathfindAlgorithmBuilder.AssignExtended);
            _potentialDestinationNodes = ActionSet.VarCollection.Assign("Dijkstra: Potential Destinations", ActionSet.IsGlobal, false);
            ActionSet.AddAction(_potentialDestinationNodes.SetVariable(EmptyArray()));

            ForeachBuilder getClosestNodes = new ForeachBuilder(ActionSet, _destinations);
            ActionSet.AddAction(_potentialDestinationNodes.ModifyVariable(Operation.AppendToArray, NodeFromPosition(getClosestNodes.IndexValue)));
            getClosestNodes.Finish();
        }

        public override void OnLoopEnd()
        {
            ActionSet.AddAction(_chosenDestination.SetVariable(IndexOfArrayValue(_potentialDestinationNodes.Get(), Builder.Current.Get())));
            ActionSet.AddAction(If(Compare(_chosenDestination.GetVariable(), Operator.NotEqual, Num(-1))));
            ActionSet.AddAction(Break());
            ActionSet.AddAction(End());
        }

        public override void Finished()
        {
            IndexReference newParentArray = ActionSet.VarCollection.Assign("Pathfinder: New parent array", ActionSet.IsGlobal, false);

            // Flip the parent array.
            IndexReference backTracker = ActionSet.VarCollection.Assign("Pathfinder: Backtracker", ActionSet.IsGlobal, PathfindAlgorithmBuilder.AssignExtended);
            ActionSet.AddAction(backTracker.SetVariable(Builder.Current.Get()));

            // Get the path.
            ActionSet.AddAction(While(Compare(
                backTracker.GetVariable(),
                Operator.GreaterThanOrEqual,
                Num(0)
            )));

            Element next = Builder.ParentArray.Get()[backTracker.Get()] - 1;

            ActionSet.AddAction(newParentArray.SetVariable(index: next, value: backTracker.Get() + 1));

            ActionSet.AddAction(backTracker.SetVariable(next));
            ActionSet.AddAction(Wait());
            ActionSet.AddAction(End());

            ResolveInfo.Pathfind(ActionSet, _player, PathmapObject, newParentArray.Get(), NodeArray[Builder.Current.Get()]);
        }
    }

    /// <summary>Gets an array of vectors forming a path.</summary>
    class PathfindVectorPath : PathmapPathfinderInfo
    {
        private readonly Element _destination;
        private IndexReference _finalNode;
        private IndexReference _finalPath;
        public Element Result => Append(_finalPath.Get(), _destination);

        public PathfindVectorPath(Element position, Element destination, SharedPathfinderInfoValues pathfinderValues) : base(position, pathfinderValues)
        {
            _destination = destination;
        }

        public override Element LoopCondition => throw new System.NotImplementedException();

        protected override void Init()
        {
            _finalNode = ActionSet.VarCollection.Assign("Dijkstra: Last", ActionSet.IsGlobal, PathfindAlgorithmBuilder.AssignExtended);
            _finalPath = ActionSet.VarCollection.Assign("Dijkstra: Final Path", ActionSet.IsGlobal, false);
            ActionSet.AddAction(_finalNode.SetVariable(NodeFromPosition(_destination)));
            ActionSet.AddAction(_finalPath.SetVariable(EmptyArray()));
        }

        public override void OnLoopEnd() {}

        public override void Finished()
        {
            // Backtrack parent array.
            ActionSet.AddAction(Builder.Current.SetVariable(_finalNode.Get()));

            // Get the path.
            ActionSet.AddAction(While(Compare(
                Builder.Current.GetVariable(),
                Operator.GreaterThanOrEqual,
                Num(0)
            )));

            // Add the current node to the final path.
            ActionSet.AddAction(_finalPath.ModifyVariable(Operation.AppendToArray, NodeArray[Builder.Current.Get()]));

            ActionSet.AddAction(Builder.Current.SetVariable(Builder.ParentArray.Get()[Builder.Current.Get()] - 1));
            ActionSet.AddAction(End());
        }
    }
}