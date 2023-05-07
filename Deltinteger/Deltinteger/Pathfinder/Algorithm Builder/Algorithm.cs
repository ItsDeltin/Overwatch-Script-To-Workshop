using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    class PathfindAlgorithmBuilder
    {
        public const bool AssignExtended = false;
        static readonly Element _leastNot0 = Num(0.0001);

        public IPathfinderInfo Info { get; }
        readonly ResolveInfoComponent resolveInfo;

        ActionSet actionSet => Info.ActionSet;
        Element nodes => Info.NodeArray;
        Element segments => Info.SegmentArray;
        Element attributes => Info.AttributeArray;

        // * workshop variables *
        /// <summary>The index of the current node being looped on.</summary>
        public IndexReference Current { get; set; }
        /// <summary>The array of indices of a node's parent.</summary>
        public IndexReference ParentArray { get; set; }
        /// <summary>The array of distances between a node and its parent.</summary>
        public IndexReference Distances { get; set; }
        /// <summary>An array of unvisited node indices.</summary>
        public IndexReference Unvisited { get; set; }

        public PathfindAlgorithmBuilder(IPathfinderInfo pathfinderInfo)
        {
            Info = pathfinderInfo;
            resolveInfo = actionSet.DeltinScript.GetComponent<ResolveInfoComponent>();

            Current = actionSet.VarCollection.Assign("Dijkstra: Current", actionSet.IsGlobal, AssignExtended);
            Distances = actionSet.VarCollection.Assign("Dijkstra: Distances", actionSet.IsGlobal, false);
            Unvisited = actionSet.VarCollection.Assign("Dijkstra: Unvisited", actionSet.IsGlobal, false);
            ParentArray = actionSet.VarCollection.Assign("Dijkstra: Parent Array", actionSet.IsGlobal, false);
        }

        public void Get()
        {
            IndexReference neighborIndex = actionSet.VarCollection.Assign("Dijkstra: Neighbor Index", actionSet.IsGlobal, AssignExtended);
            IndexReference neighborDistance = actionSet.VarCollection.Assign("Dijkstra: Distance", actionSet.IsGlobal, AssignExtended);
            IndexReference neighborSegmentAttributes = actionSet.VarCollection.Assign("Dijkstra: Neighbor Attributes", actionSet.IsGlobal, AssignExtended);
            InitializeVariables();

            actionSet.AddAction(While(Info.LoopCondition));

            // Invoke LoopStart
            Info.OnLoop();

            // Get neighboring indexes
            var connectedSegments = actionSet.VarCollection.Assign("Dijkstra: Connected Segments", actionSet.IsGlobal, AssignExtended);
            connectedSegments.Set(actionSet, GetConnectedSegments());

            // Loop through neighboring indexes
            ForeachBuilder forBuilder = new ForeachBuilder("neighborIndex", actionSet, connectedSegments.GetVariable());

            // Invoke OnConnectLoop
            Info.OnConnectLoop();

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Get the index from the segment data
                neighborIndex.SetVariable(
                    FirstOf(Filter(
                        BothNodes((Element)forBuilder.IndexValue),
                        Compare(ArrayElement(), Operator.NotEqual, Current.GetVariable())
                    ))
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    DistanceBetween(
                        nodes[neighborIndex.Get()],
                        nodes[Current.Get()]
                    ) + Distances.Get()[Current.Get()]
                )
            ));

            // Get the attributes from the current node to the neighbor node.
            actionSet.AddAction(neighborSegmentAttributes.SetVariable(Filter(attributes,
                And(
                    Compare(YOf(ArrayElement()), Operator.Equal, Current.Get()),
                    Compare(XOf(ArrayElement()), Operator.Equal, neighborIndex.Get())
                )
            )));

            string ifComment =
@"If the distance between this node and the neighbor node is lower than the node's current parent,
then the current node is closer and should be set as the neighbor's parent.
Alternatively, if the neighbor's distance is 0, that means it was not set so this should
be set as the parent regardless.

Additionally, make sure that any of the neighbor's attributes is in the attribute array.";

            // Set the current neighbor's distance if the new distance is less than what it is now.
            actionSet.AddAction(ifComment, If(And(
                Or(
                    Not(Distances.Get()[neighborIndex.Get()]),
                    neighborDistance.Get() < Distances.Get()[neighborIndex.Get()]
                ),
                Or(
                    // There are no attributes.
                    Not(CountOf(neighborSegmentAttributes.Get())),
                    // There are attributes and the attribute array contains one of the attributes.
                    Any(
                        neighborSegmentAttributes.Get(),
                        Contains(Info.EnabledAttributes, ZOf(ArrayElement()))
                    )
                )
            )));

            actionSet.AddAction(
                "Set the neighbor's distance to be the distance between the current node and neighbor node.",
                Distances.SetVariable(neighborDistance.Get(), index: neighborIndex.Get())
            );
            actionSet.AddAction(
@"Set the neighbor's parent ('parentArray[neighborIndex]') to be current. 1 is added to current because
0 means no parent was set yet (the first node will have current equal 0). This value will be subtracted
back by 1 when used.",
                ParentArray.SetVariable(Current.Get() + 1, index: neighborIndex.Get())
            );

            actionSet.AddAction(End()); // End the if.
            forBuilder.Finish(); // End the for.
            actionSet.AddAction(Unvisited.ModifyVariable(Operation.RemoveFromArrayByValue, Current.Get())); // Remove the current node from the unvisited array.
            Info.OnLoopEnd(); // External end loop logic.
            Current.Set(actionSet, LowestUnvisited()); // Set current to the unvisited node with the lowest distance.
            actionSet.AddAction(End()); // End the while loop.
            Info.Finished(); // Done.

            // Reset variables.
            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Current.SetVariable(0),
                // Distances.SetVariable(0),
                // neighborIndex.SetVariable(0),
                // neighborDistance.SetVariable(0),
                // ParentArray.SetVariable(0)
                Current.SetVariable(0),
                Distances.SetVariable(0),
                connectedSegments.SetVariable(0),
                neighborIndex.SetVariable(0),
                neighborDistance.SetVariable(0),
                ParentArray.SetVariable(0)
            ));
        }

        /// <summary>Initializes the variables used by the pathfiner.</summary>
        void InitializeVariables()
        {
            // Set current to the initial node.
            Current.Set(actionSet, "Set current to the first node.", Info.InitialNode);

            // Make sure distances[current] does not equal 0.
            Distances.Set(actionSet, _leastNot0, index: Current.Get());

            // Set the unvisited array.
            // Create an array counting up to the number of values in the nodeArray array.
            // For example, if nodeArray has 6 variables unvisitedVar will be set to [0, 1, 2, 3, 4, 5].
            Element array = Map(nodes, ArrayIndex());

            // If any of the nodes are null, destroy them.
            if (resolveInfo.PotentiallyNullNodes)
                array = Filter(array, Compare(nodes[ArrayElement()], Operator.NotEqual, Null()));

            Unvisited.Set(actionSet, array);
        }

        /// <summary>Gets the segments connected to the current node.</summary>
        Element GetConnectedSegments() => Filter(
            segments,
            // Make sure one of the segments nodes is the current node.
            Contains(BothNodes(Element.ArrayElement()), Current.Get())
        );

        /// <summary>Gets the unvisited node with the lowest distance.</summary>
        Element LowestUnvisited() => FirstOf(Sort(
            Filter(Unvisited.Get(), Compare(Distances.Get()[ArrayElement()], Operator.NotEqual, Num(0))),
            Distances.Get()[ArrayElement()]
        ));

        public static Element BothNodes(Element segment) => Element.CreateAppendArray(Node1(segment), Node2(segment));
        public static Element Node1(Element segment) => XOf(segment);
        public static Element Node2(Element segment) => YOf(segment);

        public Element NoAccessableUnvisited() => All(Unvisited.GetVariable(), Compare(Distances.Get()[ArrayElement()], Operator.Equal, Num(0)));
        public Element AnyAccessableUnvisited() => Any(Unvisited.Get(), Distances.Get()[ArrayElement()]);
    }
}