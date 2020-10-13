using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public abstract class DijkstraBase
    {
        private static readonly Element LeastNot0 = Element.Num(0.0001);

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

        public Action<ActionSet> OnLoop { get; set; } = actionSet => {
            actionSet.AddAction(Element.Wait());
        };
        public Action<ActionSet> OnConnectLoop { get; set; } = actionSet => {
            actionSet.AddAction(Element.Wait());
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
            
            current                          = actionSet.VarCollection.Assign("Dijkstra: Current", actionSet.IsGlobal, assignExtended);
            distances                        = actionSet.VarCollection.Assign("Dijkstra: Distances", actionSet.IsGlobal, false);
            unvisited                        = actionSet.VarCollection.Assign("Dijkstra: Unvisited", actionSet.IsGlobal, false);
            IndexReference connectedSegments = actionSet.VarCollection.Assign("Dijkstra: Connected Segments", actionSet.IsGlobal, assignExtended);
            IndexReference neighborIndex     = actionSet.VarCollection.Assign("Dijkstra: Neighbor Index", actionSet.IsGlobal, assignExtended);
            IndexReference neighborDistance  = actionSet.VarCollection.Assign("Dijkstra: Distance", actionSet.IsGlobal, assignExtended);
            IndexReference neighborSegmentAttributes = actionSet.VarCollection.Assign("Dijkstra: Neighbor Attributes", actionSet.IsGlobal, assignExtended);
            parentArray                      = GetParentArray();

            // Set the current variable as the first node.
            actionSet.AddAction(current.SetVariable(firstNode));
            SetInitialDistances(actionSet, distances, (Element)current.GetVariable());
            SetInitialUnvisited();

            actionSet.AddAction(Element.While(LoopCondition()));

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
                    Element.FirstOf(Element.Filter(
                        BothNodes(forBuilder.IndexValue),
                        Element.Compare(
                            Element.ArrayElement(),
                            Operator.NotEqual,
                            current.GetVariable()
                        )
                    ))
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    Element.DistanceBetween(
                        Nodes[neighborIndex.Get()],
                        Nodes[current.Get()]
                    ) + distances.Get()[current.Get()]
                )
            ));

            // Get the attributes from the current node to the neighbor node.
            actionSet.AddAction(neighborSegmentAttributes.SetVariable(Element.Filter(Attributes,
                Element.And(
                    Element.Compare(
                        Element.YOf(Element.ArrayElement()),
                        Operator.Equal,
                        current.Get()
                    ),
                    Element.Compare(
                        Element.XOf(Element.ArrayElement()),
                        Operator.Equal,
                        neighborIndex.Get()
                    )
                )
            )));

            // Set the current neighbor's distance if the new distance is less than what it is now.
            actionSet.AddAction(Element.If(Element.And(
                Element.Or(
                    Element.Compare(
                        ((Element)distances.GetVariable())[(Element)neighborIndex.GetVariable()],
                        Operator.Equal,
                        Element.Num(0)
                    ),
                    neighborDistance.Get() < distances.Get()[neighborIndex.Get()]
                ),
                Element.Or(
                    // There are no attributes.
                    Element.Not(Element.CountOf(neighborSegmentAttributes.Get())),
                    // There are attributes and the attribute array contains one of the attributes.
                    Element.Any(
                        neighborSegmentAttributes.Get(),
                        Element.Contains(attributes, Element.ZOf(Element.ArrayElement()))
                    ) 
                )
            )));

            actionSet.AddAction(distances.SetVariable((Element)neighborDistance.GetVariable(), null, (Element)neighborIndex.GetVariable()));
            actionSet.AddAction(parentArray.SetVariable((Element)current.GetVariable() + 1, null, (Element)neighborIndex.GetVariable()));

            // End the if.
            actionSet.AddAction(Element.End());
            // End the for.
            forBuilder.Finish();

            // Remove the current node from the unvisited array.
            actionSet.AddAction(unvisited.ModifyVariable(Operation.RemoveFromArrayByValue, (Element)current.GetVariable()));
            EndLoop();
            actionSet.AddAction(current.SetVariable(LowestUnvisited(Nodes, (Element)distances.GetVariable(), (Element)unvisited.GetVariable())));

            actionSet.AddAction(Element.End());

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

        protected virtual void EndLoop() {}

        protected virtual IndexReference GetParentArray() => actionSet.VarCollection.Assign("Dijkstra: Parent Array", actionSet.IsGlobal, false);
        protected virtual IndexReference GetParentAttributeArray() => actionSet.VarCollection.Assign("Dijkstra: Parent Attribute Info", actionSet.IsGlobal, false);

        protected void Backtrack(Element startNode, IndexReference finalPath, bool reversePath = false)
        {
            actionSet.AddAction(current.SetVariable(startNode));
            actionSet.AddAction(finalPath.SetVariable(Element.EmptyArray()));

            // Get the path.
            actionSet.AddAction(Element.While(Element.Compare(
                current.GetVariable(),
                Operator.GreaterThanOrEqual,
                Element.Num(0)
            )));

            Element nextNode = Nodes[(Element)current.GetVariable()];

            // For debugging generated path.
            // actionSet.AddAction(Element.Part<A_CreateEffect>(
            //     Element.Part<V_AllPlayers>(),
            //     EnumData.GetEnumValue(Effect.Orb),
            //     EnumData.GetEnumValue(Color.SkyBlue),
            //     next,
            //     new NumberElement(0.5),
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
                actionSet.AddAction(finalPath.SetVariable(Element.Append(nextNode, finalPath.Get())));
            }

            actionSet.AddAction(current.SetVariable(parentArray.Get()[current.GetVariable()] - 1));
            actionSet.AddAction(Element.End());
        }

        /// <summary>Gets the closest node to a position.</summary>
        /// <returns>The closest node as an index of the `Nodes` array.</returns>
        public static Element ClosestNodeToPosition(Element nodes, Element position, bool potentiallyNullNodes)
        {
            Element sortArray = nodes;
            if (potentiallyNullNodes) sortArray = Element.Filter(nodes, Element.Compare(Element.ArrayElement(), Operator.NotEqual, Element.Null()));

            return Element.IndexOfArrayValue(
                nodes,
                Element.FirstOf(
                    Element.Sort(
                        sortArray,
                        Element.DistanceBetween(
                            position,
                            Element.ArrayElement()
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
            Element array = Element.Map(Nodes, Element.ArrayIndex());

            // If any of the nodes are null, destroy them.
            if (resolveInfo.PotentiallyNullNodes)
                array = Element.Filter(array, Element.Compare(Nodes[Element.ArrayElement()], Operator.NotEqual, Element.Null()));
            
            actionSet.AddAction(unvisited.SetVariable(array));
        }

        private Element GetConnectedSegments(Element segments, Element currentIndex) => Element.Filter(
            segments,
            // Make sure one of the segments nodes is the current node.
            Element.Contains(
                BothNodes(Element.ArrayElement()),
                currentIndex
            )
        );

        private static Element LowestUnvisited(Element nodes, Element distances, Element unvisited) => Element.FirstOf(Element.Sort(
            Element.Filter(unvisited, Element.Compare(distances[Element.ArrayElement()], Operator.NotEqual, Element.Num(0))),
            distances[Element.ArrayElement()]
        ));

        protected Element NoAccessableUnvisited() => Element.All(unvisited.GetVariable(), Element.Compare(distances.Get()[Element.ArrayElement()], Operator.Equal, Element.Num(0)));
        protected Element AnyAccessableUnvisited() => Element.Any(unvisited.GetVariable(), Element.Compare(distances.Get()[Element.ArrayElement()], Operator.NotEqual, Element.Num(0)));

        public static Element BothNodes(Element segment) => Element.CreateAppendArray(Node1(segment), Node2(segment));
        public static Element Node1(Element segment) => Element.XOf(segment);
        public static Element Node2(Element segment) => Element.YOf(segment);
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
            actionSet.AddAction(finalPath.SetVariable(Element.EmptyArray()));
        }

        override protected Element LoopCondition() => Element.Contains(
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
            actionSet.AddAction(finalPath.SetVariable(Element.EmptyArray()));
            if (useAttributes)
            {
                finalPathAttributes = actionSet.VarCollection.Assign("Dijkstra: Final Path Attributes", actionSet.IsGlobal, false);
                actionSet.AddAction(finalPathAttributes.SetVariable(Element.EmptyArray()));
            }
        }

        protected override void EndLoop()
        {
            // Break out of the while loop when the current node is the closest node to the player.
            PlayerNodeReachedBreak = new SkipStartMarker(actionSet, Element.Compare(
                GetClosestNode(actionSet, Nodes, Element.PositionOf(player)),
                Operator.NotEqual,
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
            actionSet.AddAction(closestNodesToPlayers.SetVariable(Element.EmptyArray()));

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, players);

            actionSet.AddAction(closestNodesToPlayers.ModifyVariable(Operation.AppendToArray, GetClosestNode(actionSet, Nodes, getClosestNodes.IndexValue)));

            getClosestNodes.Finish();
        }

        override protected Element LoopCondition()
        {
            return Element.Any(
                closestNodesToPlayers.GetVariable(),
                Element.Contains(
                    unvisited.GetVariable(),
                    Element.ArrayElement()
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

        public DijkstraEither(ActionSet actionSet, Element pathfindObject, Element player, Element destinations, Element attributes) : base(actionSet, pathfindObject, Element.PositionOf(player), attributes)
        {
            this.destinations = destinations;
            this.player = player;
            reverseAttributes = true;
        }

        protected override void Assign()
        {
            potentialDestinations = actionSet.VarCollection.Assign("Dijkstra: Potential Destinations", actionSet.IsGlobal, false);
            actionSet.AddAction(potentialDestinations.SetVariable(Element.EmptyArray()));
            chosenDestination = actionSet.VarCollection.Assign("Dijkstra: Chosen Destination", actionSet.IsGlobal, assignExtended);

            ForeachBuilder getClosestNodes = new ForeachBuilder(actionSet, destinations);
            actionSet.AddAction(potentialDestinations.ModifyVariable(Operation.AppendToArray, GetClosestNode(actionSet, Nodes, getClosestNodes.IndexValue)));
            getClosestNodes.Finish();
        }

        protected override void EndLoop()
        {
            actionSet.AddAction(chosenDestination.SetVariable(Element.IndexOfArrayValue(potentialDestinations.GetVariable(), current.GetVariable())));
            actionSet.AddAction(Element.If(Element.Compare(chosenDestination.GetVariable(), Operator.NotEqual, Element.Num(-1))));
            actionSet.AddAction(Element.Part("Break"));
            actionSet.AddAction(Element.End());
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
            actionSet.AddAction(Element.While(Element.Compare(
                backTracker.GetVariable(),
                Operator.GreaterThanOrEqual,
                Element.Num(0)
            )));

            Element next = parentArray.Get()[backTracker.Get()] - 1;

            actionSet.AddAction(newParentArray.SetVariable(index:next, value: backTracker.Get() + 1));

            actionSet.AddAction(backTracker.SetVariable(next));
            actionSet.AddAction(Element.Wait()); // TODO: Should there be a minwait here?
            actionSet.AddAction(Element.End());

            actionSet.AddAction(parentArray.SetVariable(newParentArray.Get()));

            resolveInfo.Pathfind(actionSet, player, pathmapObject, parentArray.Get(), Nodes[current.Get()]);
        }

        // Loop until any of the destinations have been visited.
        protected override Element LoopCondition() => Element.All(
            potentialDestinations.GetVariable(),
            Element.Contains(
                unvisited.GetVariable(),
                Element.ArrayElement()
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
                return Element.CountOf(unvisited.GetVariable()) > 0;
            else
                return Element.Contains(
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
            actionSet.AddAction(Element.If(NoAccessableUnvisited()));
            actionSet.AddAction(Element.Part("Break"));
            actionSet.AddAction(Element.End());
        }

        protected override void GetResult()
        {
             // Save parent arrays.
            PathResolveClass.ParentArray.Set(actionSet, ClassReference.Get(), parentArray.Get());
        }
    }
}