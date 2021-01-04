using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public abstract class DijkstraBase
    {
        private static readonly V_Number LeastNot0 = new V_Number(0.0001);

        protected ActionSet actionSet { get; }
        protected Element pathmapObject { get; }
        protected Element Nodes { get; }
        protected Element Segments { get; }
        protected Element Attributes { get; }
        protected Element Source { get; }
        private Element attributes { get; }
        protected bool useAttributes { get; }
        protected bool reverseAttributes { get; set; } = false;
        protected ResolveInfoComponent resolveInfo { get; }

        protected IndexReference unvisited { get; private set; }
        protected IndexReference current { get; set; }
        protected IndexReference distances { get; set; }
        protected IndexReference parentArray { get; set; }
        protected static bool assignExtended = false;

        public Action<ActionSet> OnLoop { get; set; } = actionSet =>
        {
            actionSet.AddAction(A_Wait.MinimumWait);
        };
        public Action<ActionSet> OnConnectLoop { get; set; } = actionSet =>
        {
            actionSet.AddAction(A_Wait.MinimumWait);
        };
        public Func<ActionSet, Element, Element, Element> GetClosestNode { get; set; }

        public DijkstraBase(ActionSet actionSet, Element pathmapObject, Element position, Element attributes)
        {
            this.actionSet = actionSet;
            this.pathmapObject = pathmapObject;
            this.Source = position;
            this.attributes = attributes;
            this.useAttributes = attributes != null;

            // Set closest node determiner.
            GetClosestNode = (actionSet, nodes, position) => ClosestNodeToPosition(nodes, position, resolveInfo.PotentiallyNullNodes);

            // Get the pathmap class instance.
            PathmapClass pathmapClass = actionSet.Translate.DeltinScript.Types.GetCodeType<PathmapClass>();

            Nodes = pathmapClass.Nodes.Get()[pathmapObject];
            Segments = pathmapClass.Segments.Get()[pathmapObject];
            Attributes = pathmapClass.Attributes.Get()[pathmapObject];

            // Get the resolve info component.
            resolveInfo = actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();
        }

        public void Get()
        {
            var firstNode = GetClosestNode(actionSet, Nodes, Source);

            Assign();

            current = actionSet.VarCollection.Assign("Dijkstra: Current", actionSet.IsGlobal, assignExtended);
            distances = actionSet.VarCollection.Assign("Dijkstra: Distances", actionSet.IsGlobal, false);
            unvisited = actionSet.VarCollection.Assign("Dijkstra: Unvisited", actionSet.IsGlobal, false);
            IndexReference connectedSegments = actionSet.VarCollection.Assign("Dijkstra: Connected Segments", actionSet.IsGlobal, assignExtended);
            IndexReference neighborIndex = actionSet.VarCollection.Assign("Dijkstra: Neighbor Index", actionSet.IsGlobal, assignExtended);
            IndexReference neighborDistance = actionSet.VarCollection.Assign("Dijkstra: Distance", actionSet.IsGlobal, assignExtended);
            IndexReference neighborSegmentAttributes = actionSet.VarCollection.Assign("Dijkstra: Neighbor Attributes", actionSet.IsGlobal, assignExtended);
            parentArray = GetParentArray();

            // Set the current variable as the first node.
            actionSet.AddAction(current.SetVariable(firstNode));
            SetInitialDistances(actionSet, distances, (Element)current.GetVariable());
            SetInitialUnvisited();

            actionSet.AddAction(Element.Part<A_While>(LoopCondition()));

            // Invoke LoopStart
            OnLoop.Invoke(actionSet);

            // Get neighboring indexes
            actionSet.AddAction(connectedSegments.SetVariable(GetConnectedSegments(
                Segments,
                (Element)current.GetVariable()
            )));

            // Loop through neighboring indexes
            ForeachBuilder forBuilder = new ForeachBuilder(actionSet, connectedSegments.GetVariable());

            // Invoke OnConnectLoop
            OnConnectLoop.Invoke(actionSet);

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Get the index from the segment data
                neighborIndex.SetVariable(
                    Element.Part<V_FirstOf>(Element.Part<V_FilteredArray>(
                        BothNodes(forBuilder.IndexValue),
                        new V_Compare(
                            new V_ArrayElement(),
                            Operators.NotEqual,
                            current.GetVariable()
                        )
                    ))
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    Element.Part<V_DistanceBetween>(
                        Nodes[neighborIndex.Get()],
                        Nodes[current.Get()]
                    ) + distances.Get()[current.Get()]
                )
            ));

            // Get the attributes from the current node to the neighbor node.
            actionSet.AddAction(neighborSegmentAttributes.SetVariable(Element.Part<V_FilteredArray>(Attributes,
                Element.Part<V_And>(
                    new V_Compare(
                        Element.Part<V_YOf>(new V_ArrayElement()),
                        Operators.Equal,
                        current.Get()
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
                    Element.Part<V_Not>(distances.Get()[neighborIndex.Get()]),
                    neighborDistance.Get() < distances.Get()[neighborIndex.Get()]
                ),
                Element.Part<V_Or>(
                    // There are no attributes.
                    Element.Part<V_Not>(Element.Part<V_CountOf>(neighborSegmentAttributes.Get())),
                    // There are attributes and the attribute array contains one of the attributes.
                    Element.Part<V_IsTrueForAny>(
                        neighborSegmentAttributes.Get(),
                        Element.Part<V_ArrayContains>(attributes, Element.Part<V_ZOf>(new V_ArrayElement()))
                    )
                )
            )));

            actionSet.AddAction(
                "Set the neighbor's distance to be the distance between the current node and neighbor node.",
                distances.SetVariable(neighborDistance.Get(), index: neighborIndex.Get())
            );
            actionSet.AddAction(
@"Set the neighbor's parent ('parentArray[neighborIndex]') to be current. 1 is added to current because
0 means no parent was set yet (the first node will have current equal 0). This value will be subtracted
back by 1 when used.",
                parentArray.SetVariable(current.Get() + 1, index: neighborIndex.Get())
            );

            // End the if.
            actionSet.AddAction(new A_End());
            // End the for.
            forBuilder.Finish();

            // Remove the current node from the unvisited array.
            actionSet.AddAction(unvisited.ModifyVariable(Operation.RemoveFromArrayByValue, current.Get()));
            EndLoop();
            actionSet.AddAction(current.SetVariable(LowestUnvisited(Nodes, distances.Get(), unvisited.Get())));

            actionSet.AddAction(new A_End());

            GetResult();

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                current.SetVariable(0),
                distances.SetVariable(0),
                connectedSegments.SetVariable(0),
                neighborIndex.SetVariable(0),
                neighborDistance.SetVariable(0),
                parentArray.SetVariable(0)
            ));
        }

        protected abstract void Assign();
        protected abstract Element LoopCondition();
        protected abstract void GetResult();

        protected virtual void EndLoop() { }

        protected virtual IndexReference GetParentArray() => actionSet.VarCollection.Assign("Dijkstra: Parent Array", actionSet.IsGlobal, false);
        protected virtual IndexReference GetParentAttributeArray() => actionSet.VarCollection.Assign("Dijkstra: Parent Attribute Info", actionSet.IsGlobal, false);

        protected void Backtrack(Element startNode, IndexReference finalPath, bool reversePath = false)
        {
            actionSet.AddAction(current.SetVariable(startNode));
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(new V_Compare(
                current.GetVariable(),
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            )));

            Element nextNode = Nodes[(Element)current.GetVariable()];

            // For debugging generated path.
            // actionSet.AddAction(Element.Part<A_CreateEffect>(
            //     Element.Part<V_AllPlayers>(),
            //     EnumData.GetEnumValue(Effect.Orb),
            //     EnumData.GetEnumValue(Color.SkyBlue),
            //     next,
            //     new V_Number(0.5),
            //     EnumData.GetEnumValue(EffectRev.VisibleTo)
            // ));

            if (!reversePath)
            {
                // Add the current node to the final path.
                actionSet.AddAction(finalPath.ModifyVariable(Operation.AppendToArray, nextNode));
            }
            else
            {
                // Insert the current node to the final path.
                actionSet.AddAction(finalPath.SetVariable(Element.Part<V_Append>(nextNode, finalPath.GetVariable())));
            }

            actionSet.AddAction(current.SetVariable(Element.Part<V_ValueInArray>(parentArray.GetVariable(), current.GetVariable()) - 1));
            actionSet.AddAction(new A_End());
        }

        /// <summary>Gets the closest node to a position.</summary>
        /// <returns>The closest node as an index of the `Nodes` array.</returns>
        public static Element ClosestNodeToPosition(Element nodes, Element position, bool potentiallyNullNodes)
        {
            Element sortArray = nodes;
            if (potentiallyNullNodes) sortArray = Element.Part<V_FilteredArray>(nodes, new V_Compare(new V_ArrayElement(), Operators.NotEqual, new V_Null()));

            return Element.Part<V_IndexOfArrayValue>(
                nodes,
                Element.Part<V_FirstOf>(
                    Element.Part<V_SortedArray>(
                        sortArray,
                        Element.Part<V_DistanceBetween>(
                            position,
                            new V_ArrayElement()
                        )
                    )
                )
            );
        }

        private static void SetInitialDistances(ActionSet actionSet, IndexReference distancesVar, Element currentIndex)
        {
            actionSet.AddAction(distancesVar.SetVariable(LeastNot0, null, currentIndex));
        }

        private void SetInitialUnvisited()
        {
            // Create an array counting up to the number of values in the nodeArray array.
            // For example, if nodeArray has 6 variables unvisitedVar will be set to [0, 1, 2, 3, 4, 5].
            Element array = Element.Part<V_MappedArray>(Nodes, new V_CurrentArrayIndex());

            // If any of the nodes are null, destroy them.
            if (resolveInfo.PotentiallyNullNodes)
                array = Element.Part<V_FilteredArray>(array, new V_Compare(Nodes[new V_ArrayElement()], Operators.NotEqual, new V_Null()));

            actionSet.AddAction(unvisited.SetVariable(array));
        }

        private Element GetConnectedSegments(Element segments, Element currentIndex) => Element.Part<V_FilteredArray>(
            segments,
            // Make sure one of the segments nodes is the current node.
            Element.Part<V_ArrayContains>(
                BothNodes(new V_ArrayElement()),
                currentIndex
            )
        );

        private static Element LowestUnvisited(Element nodes, Element distances, Element unvisited) => Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
            Element.Part<V_FilteredArray>(unvisited, new V_Compare(distances[new V_ArrayElement()], Operators.NotEqual, new V_Number(0))),
            distances[new V_ArrayElement()]
        ));

        protected Element NoAccessableUnvisited() => Element.Part<V_IsTrueForAll>(unvisited.GetVariable(), new V_Compare(Element.Part<V_ValueInArray>(distances.GetVariable(), new V_ArrayElement()), Operators.Equal, new V_Number(0)));
        protected Element AnyAccessableUnvisited() => Element.Part<V_IsTrueForAny>(unvisited.GetVariable(), new V_Compare(Element.Part<V_ValueInArray>(distances.GetVariable(), new V_ArrayElement()), Operators.NotEqual, new V_Number(0)));

        public static Element BothNodes(Element segment) => Element.CreateAppendArray(Node1(segment), Node2(segment));
        public static Element Node1(Element segment) => Element.Part<V_XOf>(segment);
        public static Element Node2(Element segment) => Element.Part<V_YOf>(segment);
    }

    public class DijkstraNormal : DijkstraBase
    {
        private Element destination { get; }
        private IndexReference finalNode;
        public IndexReference finalPath { get; private set; }

        public DijkstraNormal(ActionSet actionSet, Element pathmapObject, Element position, Element destination, Element attributes) : base(actionSet, pathmapObject, position, attributes)
        {
            this.destination = destination;
        }

        override protected void Assign()
        {
            var lastNode = GetClosestNode(actionSet, Nodes, destination);

            finalNode = actionSet.VarCollection.Assign("Dijkstra: Last", actionSet.IsGlobal, assignExtended);
            finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            actionSet.AddAction(finalNode.SetVariable(lastNode));
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));
        }

        override protected Element LoopCondition() => Element.Part<V_ArrayContains>(
            unvisited.GetVariable(),
            finalNode.GetVariable()
        );

        override protected void GetResult()
        {
            Backtrack((Element)finalNode.GetVariable(), finalPath);
        }
    }

    public class DijkstraPlayer : DijkstraBase
    {
        private readonly Element player;
        public IndexReference finalPath { get; private set; }
        public IndexReference finalPathAttributes { get; private set; }
        private SkipStartMarker PlayerNodeReachedBreak;

        public DijkstraPlayer(ActionSet actionSet, Element pathmapObject, Element player, Element destination, Element attributes) : base(actionSet, pathmapObject, destination, attributes)
        {
            this.player = player;
        }

        protected override void Assign()
        {
            finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));
            if (useAttributes)
            {
                finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);
                actionSet.AddAction(finalPathAttributes.SetVariable(new V_EmptyArray()));
            }
        }

        protected override void EndLoop()
        {
            // Break out of the while loop when the current node is the closest node to the player.
            PlayerNodeReachedBreak = new SkipStartMarker(actionSet, new V_Compare(
                GetClosestNode(actionSet, Nodes, Element.Part<V_PositionOf>(player)),
                Operators.NotEqual,
                current.GetVariable()
            ));
            actionSet.AddAction(PlayerNodeReachedBreak);
        }

        protected override void GetResult()
        {
            SkipEndMarker endLoop = new SkipEndMarker();
            actionSet.AddAction(endLoop);
            PlayerNodeReachedBreak.SetEndMarker(endLoop);

            resolveInfo.Pathfind(actionSet, player, pathmapObject, parentArray.Get(), Source);
        }

        protected override Element LoopCondition() => AnyAccessableUnvisited();
    }

    public class DijkstraMultiSource : DijkstraBase
    {
        private Element players { get; }
        private IndexReference closestNodesToPlayers;

        public DijkstraMultiSource(ActionSet actionSet, Element pathmapObject, Element players, Element destination, Element attributes) : base(actionSet, pathmapObject, destination, attributes)
        {
            this.players = players;
        }

        override protected void Assign()
        {
            closestNodesToPlayers = actionSet.VarCollection.Assign("Dijkstra: Closest nodes", actionSet.IsGlobal, false);
            actionSet.AddAction(closestNodesToPlayers.SetVariable(Element.Part<V_EmptyArray>()));

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, players);

            actionSet.AddAction(closestNodesToPlayers.ModifyVariable(Operation.AppendToArray, GetClosestNode(actionSet, Nodes, getClosestNodes.IndexValue)));

            getClosestNodes.Finish();
        }

        override protected Element LoopCondition()
        {
            return Element.Part<V_IsTrueForAny>(
                closestNodesToPlayers.GetVariable(),
                Element.Part<V_ArrayContains>(
                    unvisited.GetVariable(),
                    new V_ArrayElement()
                )
            );
        }

        override protected void GetResult()
        {
            resolveInfo.Pathfind(actionSet, players, pathmapObject, parentArray.Get(), Source);
        }
    }

    public class DijkstraEither : DijkstraBase
    {
        private IndexReference potentialDestinations;
        public Element destinations { get; }
        public IndexReference chosenDestination { get; private set; }
        public Element PointDestination => destinations[(Element)chosenDestination.GetVariable()];
        private readonly Element player;

        public DijkstraEither(ActionSet actionSet, Element pathfindObject, Element player, Element destinations, Element attributes) : base(actionSet, pathfindObject, Element.Part<V_PositionOf>(player), attributes)
        {
            this.destinations = destinations;
            this.player = player;
            reverseAttributes = true;
        }

        protected override void Assign()
        {
            potentialDestinations = actionSet.VarCollection.Assign("Dijkstra: Potential Destinations", actionSet.IsGlobal, false);
            actionSet.AddAction(potentialDestinations.SetVariable(new V_EmptyArray()));
            chosenDestination = actionSet.VarCollection.Assign("Dijkstra: Chosen Destination", actionSet.IsGlobal, assignExtended);

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, destinations);
            actionSet.AddAction(potentialDestinations.ModifyVariable(Operation.AppendToArray, GetClosestNode(actionSet, Nodes, getClosestNodes.IndexValue)));
            getClosestNodes.Finish();
        }

        protected override void EndLoop()
        {
            actionSet.AddAction(chosenDestination.SetVariable(Element.Part<V_IndexOfArrayValue>(potentialDestinations.GetVariable(), current.GetVariable())));
            actionSet.AddAction(Element.Part<A_If>(new V_Compare(chosenDestination.GetVariable(), Operators.NotEqual, new V_Number(-1))));
            actionSet.AddAction(Element.Part<A_Break>());
            actionSet.AddAction(Element.Part<A_End>());
        }

        protected override void GetResult()
        {
            /*
                  |  |
            +--+--+--+

            */

            IndexReference newParentArray = actionSet.VarCollection.Assign("Pathfinder: New parent array", actionSet.IsGlobal, false);

            // Flip the parent array.
            IndexReference backTracker = actionSet.VarCollection.Assign("Pathfinder: Backtracker", actionSet.IsGlobal, assignExtended);
            actionSet.AddAction(backTracker.SetVariable(current.Get()));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(new V_Compare(
                backTracker.GetVariable(),
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            )));

            Element next = parentArray.Get()[backTracker.Get()] - 1;

            actionSet.AddAction(newParentArray.SetVariable(index: next, value: backTracker.Get() + 1));

            actionSet.AddAction(backTracker.SetVariable(next));
            actionSet.AddAction(A_Wait.MinimumWait); // TODO: Should there be a minwait here?
            actionSet.AddAction(new A_End());

            actionSet.AddAction(parentArray.SetVariable(newParentArray.Get()));

            resolveInfo.Pathfind(actionSet, player, pathmapObject, parentArray.Get(), Nodes[current.Get()]);
        }

        // Loop until any of the destinations have been visited.
        protected override Element LoopCondition() => Element.Part<V_IsTrueForAll>(
            potentialDestinations.GetVariable(),
            Element.Part<V_ArrayContains>(
                unvisited.GetVariable(),
                new V_ArrayElement()
            )
        );
    }

    public class ResolveDijkstra : DijkstraBase
    {
        // The destination to resolve the path to. Can be null.
        private Element Destination;
        // A reference to the PathResolveClass instance.
        private PathResolveClass PathResolveClass;
        // The created PathResolve's reference.
        public IndexReference ClassReference { get; private set; }
        private IndexReference finalNode;

        public ResolveDijkstra(ActionSet actionSet, Element position, Element attributes) : base(actionSet, (Element)actionSet.CurrentObject, position, attributes)
        {
        }
        public ResolveDijkstra(ActionSet actionSet, Element position, Element destination, Element attributes) : base(actionSet, (Element)actionSet.CurrentObject, position, attributes)
        {
            Destination = destination;
        }

        protected override Element LoopCondition()
        {
            if (Destination == null)
                return Element.Part<V_CountOf>(unvisited.GetVariable()) > 0;
            else
                return Element.Part<V_ArrayContains>(
                    unvisited.GetVariable(),
                    finalNode.GetVariable()
                );
        }

        protected override void Assign()
        {
            // Get the PathResolveClass instance.
            PathResolveClass = actionSet.Translate.DeltinScript.Types.GetInstance<PathResolveClass>();
            PathResolveClass.WorkshopInit(actionSet.Translate.DeltinScript);

            // Create a new PathResolve class instance.
            ClassReference = PathResolveClass.Create(actionSet, actionSet.Translate.DeltinScript.GetComponent<ClassData>());

            // Save the pathmap.
            PathResolveClass.Pathmap.Set(actionSet, ClassReference.Get(), (Element)actionSet.CurrentObject);

            // Save the destination.
            PathResolveClass.Destination.Set(actionSet, ClassReference.Get(), Source);

            // Assign FinalNode
            if (Destination != null)
            {
                finalNode = actionSet.VarCollection.Assign("Final Node", actionSet.IsGlobal, assignExtended);
                finalNode.SetVariable(GetClosestNode(actionSet, Nodes, Destination));
            }
        }

        // Override GetParentArray and GetParentAttributeArray so the index reference to the PathResolveClass variable is used.
        // protected override IndexReference GetParentArray() => PathResolveClass.ParentArray.Spot((Element)ClassReference.GetVariable());
        // protected override IndexReference GetParentAttributeArray() => PathResolveClass.ParentAttributeArray.Spot((Element)ClassReference.GetVariable());

        protected override void EndLoop()
        {
            actionSet.AddAction(Element.Part<A_If>(NoAccessableUnvisited()));
            actionSet.AddAction(Element.Part<A_Break>());
            actionSet.AddAction(Element.Part<A_End>());
        }

        protected override void GetResult()
        {
            // Save parent arrays.
            PathResolveClass.ParentArray.Set(actionSet, ClassReference.Get(), parentArray.Get());
        }
    }
}