using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    class PathfindAlgorithmBuilder
    {
        const bool _assignExtended = false;
        static readonly V_Number _leastNot0 = new V_Number(0.0001);

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

            Current = actionSet.VarCollection.Assign("Dijkstra: Current", actionSet.IsGlobal, _assignExtended);
            Distances = actionSet.VarCollection.Assign("Dijkstra: Distances", actionSet.IsGlobal, false);
            Unvisited = actionSet.VarCollection.Assign("Dijkstra: Unvisited", actionSet.IsGlobal, false);
            ParentArray = actionSet.VarCollection.Assign("Dijkstra: Parent Array", actionSet.IsGlobal, false);
        }

        public void Get()
        {
            IndexReference neighborIndex = actionSet.VarCollection.Assign("Dijkstra: Neighbor Index", actionSet.IsGlobal, _assignExtended);
            IndexReference neighborDistance = actionSet.VarCollection.Assign("Dijkstra: Distance", actionSet.IsGlobal, _assignExtended);
            IndexReference neighborSegmentAttributes = actionSet.VarCollection.Assign("Dijkstra: Neighbor Attributes", actionSet.IsGlobal, _assignExtended);
            InitializeVariables();

            actionSet.AddAction(Element.Part<A_While>(Info.LoopCondition));

            // Invoke LoopStart
            Info.OnLoop();

            // Get neighboring indexes
            var connectedSegments = actionSet.VarCollection.Assign("Dijkstra: Connected Segments", actionSet.IsGlobal, _assignExtended);
            connectedSegments.Set(actionSet, GetConnectedSegments());

            // Loop through neighboring indexes
            ForeachBuilder forBuilder = new ForeachBuilder(actionSet, connectedSegments.GetVariable());

            // Invoke OnConnectLoop
            Info.OnConnectLoop();

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Get the index from the segment data
                neighborIndex.SetVariable(
                    Element.Part<V_FirstOf>(Element.Part<V_FilteredArray>(
                        BothNodes(forBuilder.IndexValue),
                        new V_Compare(
                            new V_ArrayElement(),
                            Operators.NotEqual,
                            Current.GetVariable()
                        )
                    ))
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    Element.Part<V_DistanceBetween>(
                        nodes[neighborIndex.Get()],
                        nodes[Current.Get()]
                    ) + Distances.Get()[Current.Get()]
                )
            ));

            // Get the attributes from the current node to the neighbor node.
            actionSet.AddAction(neighborSegmentAttributes.SetVariable(Element.Part<V_FilteredArray>(attributes,
                Element.Part<V_And>(
                    new V_Compare(
                        Element.Part<V_YOf>(new V_ArrayElement()),
                        Operators.Equal,
                        Current.Get()
                    ),
                    new V_Compare(
                        Element.Part<V_XOf>(new V_ArrayElement()),
                        Operators.Equal,
                        neighborIndex.Get()
                    )
                )
            )));

            string ifComment =
@"If the distance between this node and the neighbor node is lower than the node's current parent,
then the current node is closer and should be set as the neighbor's parent.
Alternatively, if the neighbor's distance is 0, that means it was not set so this should
be set as the parent regardless.

Additionally, make sure that any of the neighbor's attributes is in the attribute array.";

            // Set the current neighbor's distance if the new distance is less than what it is now.
            actionSet.AddAction(ifComment, Element.Part<A_If>(Element.Part<V_And>(
                Element.Part<V_Or>(
                    Element.Part<V_Not>(Distances.Get()[neighborIndex.Get()]),
                    neighborDistance.Get() < Distances.Get()[neighborIndex.Get()]
                ),
                Element.Part<V_Or>(
                    // There are no attributes.
                    Element.Part<V_Not>(Element.Part<V_CountOf>(neighborSegmentAttributes.Get())),
                    // There are attributes and the attribute array contains one of the attributes.
                    Element.Part<V_IsTrueForAny>(
                        neighborSegmentAttributes.Get(),
                        Element.Part<V_ArrayContains>(Info.EnabledAttributes, Element.Part<V_ZOf>(new V_ArrayElement()))
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

            actionSet.AddAction(new A_End()); // End the if.
            forBuilder.Finish(); // End the for.
            actionSet.AddAction(Unvisited.ModifyVariable(Operation.RemoveFromArrayByValue, Current.Get())); // Remove the current node from the unvisited array.
            Info.OnLoopEnd(); // External end loop logic.
            Current.Set(actionSet, LowestUnvisited()); // Set current to the unvisited node with the lowest distance.
            actionSet.AddAction(new A_End()); // End the while loop.
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
            Element array = Element.Part<V_MappedArray>(nodes, new V_CurrentArrayIndex());

            // If any of the nodes are null, destroy them.
            if (resolveInfo.PotentiallyNullNodes)
                array = Element.Part<V_FilteredArray>(array, new V_Compare(nodes[new V_ArrayElement()], Operators.NotEqual, new V_Null()));

            Unvisited.Set(actionSet, array);
        }

        /// <summary>Gets the segments connected to the current node.</summary>
        Element GetConnectedSegments() => Element.Part<V_FilteredArray>(
            segments,
            // Make sure one of the segments nodes is the current node.
            Element.Part<V_ArrayContains>(
                BothNodes(new V_ArrayElement()),
                Current.Get()
            )
        );

        /// <summary>Gets the unvisited node with the lowest distance.</summary>
        Element LowestUnvisited() => Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
            Element.Part<V_FilteredArray>(Unvisited.Get(), new V_Compare(Distances.Get()[new V_ArrayElement()], Operators.NotEqual, new V_Number(0))),
            Distances.Get()[new V_ArrayElement()]
        ));

        static Element BothNodes(Element segment) => Element.CreateAppendArray(Node1(segment), Node2(segment));
        static Element Node1(Element segment) => Element.Part<V_XOf>(segment);
        static Element Node2(Element segment) => Element.Part<V_YOf>(segment);

        public Element NoAccessableUnvisited() => Element.Part<V_IsTrueForAll>(Unvisited.GetVariable(), new V_Compare(Element.Part<V_ValueInArray>(Distances.GetVariable(), new V_ArrayElement()), Operators.Equal, new V_Number(0)));
        public Element AnyAccessableUnvisited() => Element.Part<V_IsTrueForAny>(Unvisited.Get(), Distances.Get()[new V_ArrayElement()]);
    }
}