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
        protected Element position { get; }
        private Element attributes { get; }
        protected bool useAttributes { get; }

        protected IndexReference unvisited { get; private set; }
        protected IndexReference current { get; set; }
        protected IndexReference distances { get; set; }
        protected IndexReference parentArray { get; set; }
        protected IndexReference parentAttributeInfo { get; set; }
        protected static bool assignExtended = false;

        public DijkstraBase(ActionSet actionSet, Element pathmapObject, Element position, Element attributes)
        {
            this.actionSet = actionSet;
            this.pathmapObject = pathmapObject;
            this.position = position;
            this.attributes = attributes;
            this.useAttributes = attributes != null && attributes is V_EmptyArray == false;

            PathmapClass pathmapClass = actionSet.Translate.DeltinScript.Types.GetCodeType<PathmapClass>();

            Nodes = ((Element)pathmapClass.Nodes.GetVariable())[pathmapObject];
            Segments = ((Element)pathmapClass.Segments.GetVariable())[pathmapObject];
        }

        public void Get()
        {
            var firstNode = ClosestNodeToPosition(Nodes, position);

            Assign();
            
            current                          = actionSet.VarCollection.Assign("Dijkstra: Current", actionSet.IsGlobal, assignExtended);
            distances                        = actionSet.VarCollection.Assign("Dijkstra: Distances", actionSet.IsGlobal, false);
            unvisited                        = actionSet.VarCollection.Assign("Dijkstra: Unvisited", actionSet.IsGlobal, false);
            IndexReference connectedSegments = actionSet.VarCollection.Assign("Dijkstra: Connected Segments", actionSet.IsGlobal, assignExtended);
            IndexReference neighborIndex     = actionSet.VarCollection.Assign("Dijkstra: Neighbor Index", actionSet.IsGlobal, assignExtended);
            IndexReference neighborDistance  = actionSet.VarCollection.Assign("Dijkstra: Distance", actionSet.IsGlobal, assignExtended);
            parentArray                      = GetParentArray();
            if (useAttributes) parentAttributeInfo = GetParentAttributeArray();

            // Set the current variable as the first node.
            actionSet.AddAction(current.SetVariable(firstNode));
            SetInitialDistances(actionSet, distances, (Element)current.GetVariable());
            SetInitialUnvisited(actionSet, Nodes, unvisited);

            actionSet.AddAction(Element.Part<A_While>(LoopCondition()));

            // Get neighboring indexes
            actionSet.AddAction(connectedSegments.SetVariable(GetConnectedSegments(
                Segments,
                (Element)current.GetVariable()
            )));

            actionSet.AddAction(A_Wait.MinimumWait);

            // Loop through neighboring indexes
            ForeachBuilder forBuilder = new ForeachBuilder(actionSet, connectedSegments.GetVariable());

            actionSet.AddAction(A_Wait.MinimumWait);

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
                        Nodes[(Element)neighborIndex.GetVariable()],
                        Nodes[(Element)current.GetVariable()]
                    ) + ((Element)distances.GetVariable())[(Element)current.GetVariable()]
                )
            ));

            // Set the current neighbor's distance if the new distance is less than what it is now.
            actionSet.AddAction(Element.Part<A_If>(Element.Part<V_Or>(
                new V_Compare(
                    ((Element)distances.GetVariable())[(Element)neighborIndex.GetVariable()],
                    Operators.Equal,
                    new V_Number(0)
                ),
                (Element)neighborDistance.GetVariable() < ((Element)distances.GetVariable())[(Element)neighborIndex.GetVariable()]
            )));

            actionSet.AddAction(distances.SetVariable((Element)neighborDistance.GetVariable(), null, (Element)neighborIndex.GetVariable()));
            actionSet.AddAction(parentArray.SetVariable((Element)current.GetVariable() + 1, null, (Element)neighborIndex.GetVariable()));

            if (useAttributes)
                actionSet.AddAction(parentAttributeInfo.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(
                            current.GetVariable(),
                            Operators.Equal,
                            Node1(forBuilder.IndexValue)
                        ),
                        Node1Attribute(forBuilder.IndexValue),
                        Node2Attribute(forBuilder.IndexValue)
                    ),
                    null,
                    (Element)neighborIndex.GetVariable()
                ));

            // End the if.
            actionSet.AddAction(new A_End());
            // End the for.
            forBuilder.Finish();

            // Remove the current node from the unvisited array.
            actionSet.AddAction(unvisited.ModifyVariable(Operation.RemoveFromArrayByValue, (Element)current.GetVariable()));
            EndLoop();
            actionSet.AddAction(current.SetVariable(LowestUnvisited(Nodes, (Element)distances.GetVariable(), (Element)unvisited.GetVariable())));

            actionSet.AddAction(new A_End());

            GetResult();

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                current.SetVariable(0),
                distances.SetVariable(0),
                connectedSegments.SetVariable(0),
                neighborIndex.SetVariable(0),
                neighborDistance.SetVariable(0),
                parentArray.SetVariable(0),
                parentAttributeInfo.SetVariable(0)
            ));
        }

        protected abstract void Assign();
        protected abstract Element LoopCondition();
        protected abstract void GetResult();

        protected virtual void EndLoop() {}

        protected virtual IndexReference GetParentArray() => actionSet.VarCollection.Assign("Dijkstra: Parent Array", actionSet.IsGlobal, false);
        protected virtual IndexReference GetParentAttributeArray() => actionSet.VarCollection.Assign("Dijkstra: Parent Attribute Info", actionSet.IsGlobal, false);

        protected void Backtrack(Element destination, IndexReference finalPath, IndexReference finalPathAttributes)
        {
            actionSet.AddAction(current.SetVariable(destination));
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));

            // Get the path.
            actionSet.AddAction(Element.Part<A_While>(new V_Compare(
                current.GetVariable(),
                Operators.GreaterThanOrEqual,
                new V_Number(0)
            )));

            // For debugging generated path.
            // actionSet.AddAction(Element.Part<A_CreateEffect>(
            //     Element.Part<V_AllPlayers>(),
            //     EnumData.GetEnumValue(Effect.Orb),
            //     EnumData.GetEnumValue(Color.SkyBlue),
            //     next,
            //     new V_Number(0.5),
            //     EnumData.GetEnumValue(EffectRev.VisibleTo)
            // ));

            // Add the current node to the final path.
            actionSet.AddAction(finalPath.ModifyVariable(Operation.AppendToArray, Nodes[(Element)current.GetVariable()]));
            // Add the current attribute to the final path attributes.
            if (useAttributes) actionSet.AddAction(finalPathAttributes.ModifyVariable(Operation.AppendToArray, ((Element)parentAttributeInfo.GetVariable())[(Element)current.GetVariable()]));

            actionSet.AddAction(current.SetVariable(Element.Part<V_ValueInArray>(parentArray.GetVariable(), current.GetVariable()) - 1));
            actionSet.AddAction(new A_End());
        }

        /// <summary>Gets the closest node to a position.</summary>
        /// <returns>The closest node as an index of the `Nodes` array.</returns>
        public static Element ClosestNodeToPosition(Element nodes, Element position) => Element.Part<V_IndexOfArrayValue>(
            nodes,
            Element.Part<V_FirstOf>(
                Element.Part<V_SortedArray>(
                    nodes,
                    Element.Part<V_DistanceBetween>(
                        position,
                        new V_ArrayElement()
                    )
                )
            )
        );

        private static void SetInitialDistances(ActionSet actionSet, IndexReference distancesVar, Element currentIndex)
        {
            actionSet.AddAction(distancesVar.SetVariable(LeastNot0, null, currentIndex));
        }

        private static void SetInitialUnvisited(ActionSet actionSet, Element nodeArray, IndexReference unvisitedVar)
        {
            // Create an array counting up to the number of values in the nodeArray array.
            // For example, if nodeArray has 6 variables unvisitedVar will be set to [0, 1, 2, 3, 4, 5].

            // Empty the unvisited array.
            actionSet.AddAction(unvisitedVar.SetVariable(new V_EmptyArray()));
            
            IndexReference current = actionSet.VarCollection.Assign("unvisitedBuilder", actionSet.IsGlobal, assignExtended);
            actionSet.AddAction(current.SetVariable(0));

            // While current < the count of the node array.
            actionSet.AddAction(Element.Part<A_While>((Element)current.GetVariable() < Element.Part<V_CountOf>(nodeArray)));

            actionSet.AddAction(unvisitedVar.ModifyVariable(Operation.AppendToArray, (Element)current.GetVariable()));
            actionSet.AddAction(current.ModifyVariable(Operation.Add, 1));

            // End the while.
            actionSet.AddAction(new A_End());
        }

        private Element GetConnectedSegments(Element segments, Element currentIndex)
        {
            Element currentSegmentCheck = new V_ArrayElement();

            Element useAttribute = Element.TernaryConditional(
                new V_Compare(Node1(currentSegmentCheck), Operators.Equal, currentIndex),
                Node1Attribute(currentSegmentCheck),
                Node2Attribute(currentSegmentCheck)
            );

            Element isValid;
            if (useAttributes)
                isValid = Element.Part<V_ArrayContains>(
                    Element.Part<V_Append>(attributes, new V_Number(0)),
                    useAttribute
                );
            else
                isValid = new V_Compare(useAttribute, Operators.Equal, new V_Number(0));

            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_And>(
                    // Make sure one of the segments nodes is the current node.
                    Element.Part<V_ArrayContains>(
                        BothNodes(currentSegmentCheck),
                        currentIndex
                    ),
                    isValid
                )
            );
        }

        private static Element LowestUnvisited(Element nodes, Element distances, Element unvisited) => Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
            Element.Part<V_FilteredArray>(unvisited, new V_Compare(distances[new V_ArrayElement()], Operators.NotEqual, new V_Number(0))),
            distances[new V_ArrayElement()]
        ));

        protected Element NoAccessableUnvisited() => Element.Part<V_IsTrueForAll>(unvisited.GetVariable(), new V_Compare(Element.Part<V_ValueInArray>(distances.GetVariable(), new V_ArrayElement()), Operators.Equal, new V_Number(0)));
        protected Element AnyAccessableUnvisited() => Element.Part<V_IsTrueForAny>(unvisited.GetVariable(), new V_Compare(Element.Part<V_ValueInArray>(distances.GetVariable(), new V_ArrayElement()), Operators.NotEqual, new V_Number(0)));

        private static Element BothNodes(Element segment) => Element.CreateAppendArray(Node1(segment), Node2(segment));
        private static Element Node1(Element segment) => Element.Part<V_RoundToInteger>(Element.Part<V_XOf>(segment), EnumData.GetEnumValue(Rounding.Down));
        private static Element Node2(Element segment) => Element.Part<V_RoundToInteger>(Element.Part<V_YOf>(segment), EnumData.GetEnumValue(Rounding.Down));
        private static Element Node1Attribute(Element segment) => Element.Part<V_RoundToInteger>(
            (Element.Part<V_XOf>(segment) % 1) * 100,
            EnumData.GetEnumValue(Rounding.Nearest)
        );
        private static Element Node2Attribute(Element segment) => Element.Part<V_RoundToInteger>(
            (Element.Part<V_YOf>(segment) % 1) * 100,
            EnumData.GetEnumValue(Rounding.Nearest)
        );
    }

    public class DijkstraNormal : DijkstraBase
    {
        private Element destination { get; }
        private IndexReference finalNode;
        public IndexReference finalPath { get; private set; }
        public IndexReference finalPathAttributes { get; private set; }

        public DijkstraNormal(ActionSet actionSet, Element pathmapObject, Element position, Element destination, Element attributes) : base(actionSet, pathmapObject, position, attributes)
        {
            this.destination = destination;
        }

        override protected void Assign()
        {
            var lastNode = ClosestNodeToPosition(Nodes, destination);

            finalNode = actionSet.VarCollection.Assign("Dijkstra: Last", actionSet.IsGlobal, assignExtended);
            finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            actionSet.AddAction(finalNode.SetVariable(lastNode));
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));
            if (useAttributes)
            {
                finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);
                actionSet.AddAction(finalPathAttributes.SetVariable(new V_EmptyArray()));
            }
        }

        override protected Element LoopCondition() => Element.Part<V_ArrayContains>(
            unvisited.GetVariable(),
            finalNode.GetVariable()
        );

        override protected void GetResult()
        {
            Backtrack((Element)finalNode.GetVariable(), finalPath, finalPathAttributes);
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
                ClosestNodeToPosition(Nodes, Element.Part<V_PositionOf>(player)),
                Operators.Equal,
                current.GetVariable()
            ));
            actionSet.AddAction(PlayerNodeReachedBreak);
        }

        protected override void GetResult()
        {
            SkipEndMarker endLoop = new SkipEndMarker();
            actionSet.AddAction(endLoop);
            PlayerNodeReachedBreak.SetEndMarker(endLoop);

            // TODO: Backtrack sets current as the destination parameter, but current is being sent as the destination, resulting in a useless action.
            Backtrack((Element)current.GetVariable(), finalPath, finalPathAttributes);

            actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>().Pathfind(actionSet, player, (Element)finalPath.GetVariable(), position, finalPathAttributes?.GetVariable() as Element);
        }

        protected override Element LoopCondition() => AnyAccessableUnvisited();
    }
    
    public class DijkstraMultiSource : DijkstraBase
    {
        private PathfinderInfo pathfinderInfo { get; }
        private Element players { get; }
        private IndexReference closestNodesToPlayers;

        public DijkstraMultiSource(ActionSet actionSet, PathfinderInfo pathfinderInfo, Element pathmapObject, Element players, Element destination, Element attributes) : base(actionSet, pathmapObject, destination, attributes)
        {
            this.pathfinderInfo = pathfinderInfo;
            this.players = players;
        }

        override protected void Assign()
        {
            closestNodesToPlayers = actionSet.VarCollection.Assign("Dijkstra: Closest nodes", actionSet.IsGlobal, false);
            actionSet.AddAction(closestNodesToPlayers.SetVariable(Element.Part<V_EmptyArray>()));

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, players);

            actionSet.AddAction(closestNodesToPlayers.SetVariable(
                Element.Part<V_Append>(
                    closestNodesToPlayers.GetVariable(),
                    ClosestNodeToPosition(Nodes, getClosestNodes.IndexValue)
                )
            ));

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
            ForeachBuilder assignPlayerPaths = new ForeachBuilder(actionSet, players);
            actionSet.AddAction(A_Wait.MinimumWait);

            IndexReference finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            IndexReference finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);

            actionSet.AddAction(finalPathAttributes.SetVariable(new V_EmptyArray()));

            Backtrack(
                Element.Part<V_ValueInArray>(
                    closestNodesToPlayers.GetVariable(),
                    assignPlayerPaths.Index
                ),
                finalPath,
                finalPathAttributes
            );
            actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>().Pathfind(actionSet, assignPlayerPaths.IndexValue, (Element)finalPath.GetVariable(), position, (Element)finalPathAttributes.GetVariable());
            assignPlayerPaths.Finish();
        }
    }

    public class DijkstraEither : DijkstraBase
    {
        private IndexReference potentialDestinations;
        public Element destinations { get; }
        public IndexReference finalPath { get; private set; }
        public IndexReference finalPathAttributes { get; private set; }
        public IndexReference chosenDestination { get; private set; }
        public Element PointDestination => destinations[(Element)chosenDestination.GetVariable()];

        public DijkstraEither(ActionSet actionSet, Element pathfindObject, Element position, Element destinations, Element attributes) : base(actionSet, pathfindObject, position, attributes)
        {
            this.destinations = destinations;
        }

        protected override void Assign()
        {
            potentialDestinations = actionSet.VarCollection.Assign("Dijkstra: Potential Destinations", actionSet.IsGlobal, false);
            actionSet.AddAction(potentialDestinations.SetVariable(new V_EmptyArray()));
            chosenDestination = actionSet.VarCollection.Assign("Dijkstra: Chosen Destination", actionSet.IsGlobal, assignExtended);

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, destinations);
            actionSet.AddAction(potentialDestinations.ModifyVariable(Operation.AppendToArray, ClosestNodeToPosition(Nodes, getClosestNodes.IndexValue)));
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
            finalPath = actionSet.VarCollection.Assign("Dijkstra: Final Path", actionSet.IsGlobal, false);
            actionSet.AddAction(finalPath.SetVariable(new V_EmptyArray()));
            if (useAttributes)
            {
                finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);
                actionSet.AddAction(finalPathAttributes.SetVariable(new V_EmptyArray()));
            }
            Backtrack((Element)current.GetVariable(), finalPath, finalPathAttributes);
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

            // Create a new PathResolve class instance.
            ClassReference = PathResolveClass.Create(actionSet, actionSet.Translate.DeltinScript.GetComponent<ClassData>());

            // Save the pathmap.
            PathResolveClass.Pathmap.Set(actionSet, (Element)ClassReference.GetVariable(), (Element)actionSet.CurrentObject);

            // Save the destination.
            PathResolveClass.Destination.Set(actionSet, (Element)ClassReference.GetVariable(), position);

            // Assign FinalNode
            if (Destination != null)
            {
                finalNode = actionSet.VarCollection.Assign("Final Node", actionSet.IsGlobal, assignExtended);
                finalNode.SetVariable(ClosestNodeToPosition(Nodes, Destination));
            }
        }

        // Override GetParentArray and GetParentAttributeArray so the index reference to the PathResolveClass variable is used.
        protected override IndexReference GetParentArray() => PathResolveClass.ParentArray.Spot((Element)ClassReference.GetVariable());
        protected override IndexReference GetParentAttributeArray() => PathResolveClass.ParentAttributeArray.Spot((Element)ClassReference.GetVariable());

        protected override void EndLoop()
        {
            actionSet.AddAction(Element.Part<A_If>(NoAccessableUnvisited()));
            actionSet.AddAction(Element.Part<A_Break>());
            actionSet.AddAction(Element.Part<A_End>());
        }

        protected override void GetResult() {}
    }
}