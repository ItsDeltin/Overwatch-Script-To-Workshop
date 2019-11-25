using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public abstract class DijkstraBase
    {
        private static readonly V_Number Infinity = new V_Number(9999);
        private static readonly V_Number LeastNot0 = new V_Number(0.0001);

        protected TranslateRule context { get; }
        protected PathMapVar pathmap { get; }
        protected Element position { get; }
        private Element attributes { get; }
        private bool useAttributes { get; }
        private bool reversed { get; }

        protected IndexedVar unvisited { get; private set; }
        private IndexedVar current { get; set; }
        private IndexedVar parentArray { get; set; }
        private IndexedVar parentAttributeInfo { get; set; }

        public DijkstraBase(TranslateRule context, PathMapVar pathmap, Element position, Element attributes, bool reversed)
        {
            this.context = context;
            this.pathmap = pathmap;
            this.position = position;
            this.attributes = attributes;
            this.useAttributes = attributes != null && attributes is V_EmptyArray == false;
            this.reversed = reversed;
        }

        public void Get()
        {
            var firstNode = ClosestNodeToPosition(pathmap, position);

            Assign();
            
            current                      = IndexedVar.AssignInternalVarExt(context.VarCollection, null, "Dijkstra: Current",   context.IsGlobal);
            IndexedVar distances         = IndexedVar.AssignInternalVar   (context.VarCollection, null, "Dijkstra: Distances", context.IsGlobal);
            unvisited                    = IndexedVar.AssignInternalVar   (context.VarCollection, null, "Dijkstra: Unvisited", context.IsGlobal);
            IndexedVar connectedSegments = IndexedVar.AssignInternalVarExt(context.VarCollection, null, "Dijkstra: Connected Segments", context.IsGlobal);
            IndexedVar neighborIndex     = IndexedVar.AssignInternalVarExt(context.VarCollection, null, "Dijkstra: Neighbor Index", context.IsGlobal);
            IndexedVar neighborDistance  = IndexedVar.AssignInternalVarExt(context.VarCollection, null, "Dijkstra: Distance", context.IsGlobal);
            parentArray                  = IndexedVar.AssignInternalVar   (context.VarCollection, null, "Dijkstra: Parent Array", context.IsGlobal);
            if (useAttributes)
                parentAttributeInfo = IndexedVar.AssignInternalVar(context.VarCollection, null, "Dijkstra: Parent Attribute Info", context.IsGlobal);

            // Set the current variable as the first node.
            context.Actions.AddRange(current.SetVariable(firstNode));
            SetInitialDistances(context, pathmap.PathMap, distances, current.GetVariable());
            SetInitialUnvisited(context, pathmap.PathMap, unvisited);

            WhileBuilder whileBuilder = new WhileBuilder(context, LoopCondition());
            whileBuilder.Setup();

            // Get neighboring indexes
            whileBuilder.AddActions(
                connectedSegments.SetVariable(GetConnectedSegments(
                    pathmap.Nodes.GetVariable(),
                    pathmap.Segments.GetVariable(),
                    current.GetVariable(),
                    reversed
                ))
            );

            // Loop through neighboring indexes
            ForEachBuilder forBuilder = new ForEachBuilder(context, connectedSegments.GetVariable());
            forBuilder.Setup();

            forBuilder.AddActions(ArrayBuilder<Element>.Build(
                // Get the index from the segment data
                neighborIndex.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(
                            current.GetVariable(),
                            Operators.NotEqual,
                            Node1(forBuilder.IndexValue)
                        ),
                        Node1(forBuilder.IndexValue),
                        Node2(forBuilder.IndexValue),
                        false
                    )
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    Element.Part<V_DistanceBetween>(
                        pathmap.Nodes.GetVariable()[neighborIndex.GetVariable()],
                        pathmap.Nodes.GetVariable()[current.GetVariable()]
                    ) + distances.GetVariable()[current.GetVariable()]
                )
            ));

            // Set the current neighbor's distance if the new distance is less than what it is now.
            IfBuilder ifBuilder = new IfBuilder(context.Actions,
                neighborDistance.GetVariable()
                <
                WorkingDistance(distances.GetVariable(), neighborIndex.GetVariable())
            );
            ifBuilder.Setup();

            forBuilder.AddActions(distances.SetVariable(neighborDistance.GetVariable(), null, neighborIndex.GetVariable()));
            forBuilder.AddActions(parentArray.SetVariable(current.GetVariable() + 1, null, neighborIndex.GetVariable()));

            if (useAttributes)
                forBuilder.AddActions(parentAttributeInfo.SetVariable(
                    Element.TernaryConditional(
                        new V_Compare(
                            current.GetVariable(),
                            Operators.Equal,
                            Node1(forBuilder.IndexValue)
                        ),
                        Node1Attribute(forBuilder.IndexValue),
                        Node2Attribute(forBuilder.IndexValue),
                        false
                    ),
                    null,
                    neighborIndex.GetVariable()
                ));

            ifBuilder.Finish();
            forBuilder.Finish();

            whileBuilder.AddActions(ArrayBuilder<Element>.Build(
                // Add the current to the visited array.
                unvisited.SetVariable(Element.Part<V_RemoveFromArray>(unvisited.GetVariable(), current.GetVariable())),

                // Set the current node as the smallest unvisited.
                current.SetVariable(LowestUnvisited(pathmap.Nodes.GetVariable(), distances.GetVariable(), unvisited.GetVariable()))
            ));

            whileBuilder.Finish();

            GetResult();

            context.Actions.AddRange(ArrayBuilder<Element>.Build(
                current.SetVariable(-1),
                distances.SetVariable(-1),
                unvisited.SetVariable(-1),
                connectedSegments.SetVariable(-1),
                neighborIndex.SetVariable(-1),
                neighborDistance.SetVariable(-1),
                parentArray.SetVariable(-1),
                parentAttributeInfo.SetVariable(-1)
            ));

            Reset();
        }

        protected abstract void Assign();
        protected abstract Element LoopCondition();
        protected abstract void GetResult();
        protected abstract void Reset();

        protected void Backtrack(Element destination, IndexedVar finalPath, IndexedVar finalPathAttributes)
        {
            context.Actions.AddRange(
                current.SetVariable(destination)
            );
            context.Actions.AddRange(
                finalPath.SetVariable(new V_EmptyArray())
            );

            // Get the path.
            WhileBuilder backtrack = new WhileBuilder(context, new V_Compare(
                current.GetVariable(),
                Operators.NotEqual,
                new V_Number(-1)
            ));
            backtrack.Setup();

            Element next = pathmap.Nodes.GetVariable()[current.GetVariable()];
            Element array = finalPath.GetVariable();
            Element first;
            Element second;

            Element nextAttribute = parentAttributeInfo.GetVariable()[current.GetVariable()];
            Element attributeArray = finalPathAttributes.GetVariable();
            Element firstAttribute;
            Element secondAttribute;

            if (!reversed)
            {
                first = next;
                second = array;
                firstAttribute = nextAttribute;
                secondAttribute = attributeArray;
            }
            else
            {
                first = array;
                second = next;
                firstAttribute = attributeArray;
                secondAttribute = nextAttribute;
            }

            backtrack.AddActions(finalPath.SetVariable(Element.Part<V_Append>(first, second)));
            if (useAttributes)
                backtrack.AddActions(finalPathAttributes.SetVariable(Element.Part<V_Append>(firstAttribute, secondAttribute)));
            backtrack.AddActions(current.SetVariable(parentArray.GetVariable()[current.GetVariable()] - 1));
            
            backtrack.Finish();
        }

        protected static Element ClosestNodeToPosition(PathMapVar pathmap, Element position)
        {
            return Element.Part<V_IndexOfArrayValue>(
                pathmap.Nodes.GetVariable(),
                Element.Part<V_FirstOf>(
                    Element.Part<V_SortedArray>(
                        pathmap.Nodes.GetVariable(),
                        Element.Part<V_DistanceBetween>(
                            position,
                            new V_ArrayElement()
                        )
                    )
                )
            );
        }

        private static void SetInitialDistances(TranslateRule context, PathMap pathmap, IndexedVar distancesVar, Element currentIndex)
        {
            context.Actions.AddRange(distancesVar.SetVariable(LeastNot0, null, currentIndex));
        }

        private static void SetInitialUnvisited(TranslateRule context, PathMap pathmap, IndexedVar unvisitedVar)
        {
            Element[] unvisited = new Element[pathmap.Nodes.Length];
            for (int i = 0; i < unvisited.Length; i++)
                unvisited[i] = i;
            
            context.Actions.AddRange(unvisitedVar.SetVariable(Element.CreateArray(unvisited)));
        }

        private static void SetInitialParents(TranslateRule context, PathMap pathmap, IndexedVar parentVar)
        {
            Element[] parents = new Element[pathmap.Nodes.Length];
            for (int i = 0; i < parents.Length; i++)
                parents[i] = -1;
            
            context.Actions.AddRange(parentVar.SetVariable(Element.CreateArray(parents)));
        }

        private Element GetConnectedSegments(Element nodes, Element segments, Element currentIndex, bool reversed)
        {
            Element currentSegmentCheck = new V_ArrayElement();

            Element useAttribute = Element.TernaryConditional(
                new V_Compare(Node1(currentSegmentCheck), Operators.Equal, currentIndex),
                Node1Attribute(currentSegmentCheck),
                Node2Attribute(currentSegmentCheck),
                false
            );

            Element isValid;
            if (useAttributes)
                isValid = Element.Part<V_Or>(
                    new V_Compare(useAttribute, Operators.Equal, 0),
                    Element.Part<V_ArrayContains>(
                        attributes,
                        useAttribute
                    )
                );
            else
                isValid = new V_Compare(useAttribute, Operators.Equal, 0);

            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_And>(
                    // Make sure one of the segments nodes is the current node.
                    Element.Part<V_ArrayContains>(
                        Nodes(currentSegmentCheck),
                        currentIndex
                    ),
                    isValid
                )
            );
        }

        private static Element LowestUnvisited(Element nodes, Element distances, Element unvisited)
        {
            return Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
                unvisited,
                WorkingDistance(
                    distances,
                    new V_ArrayElement()
                )
            ));
        }

        private static Element WorkingDistance(Element distances, Element index)
        {
            // Return infinity if the distance is unassigned.
            return Element.TernaryConditional(
                new V_Compare(distances[index], Operators.NotEqual, 0),
                distances[index],
                Infinity,
                false
            );
        }

        private static Element Nodes(Element segment)
        {
            return Element.CreateArray(Node1(segment), Node2(segment));
        }
        private static Element Node1(Element segment)
        {
            return Element.Part<V_RoundToInteger>(Element.Part<V_XOf>(segment), EnumData.GetEnumValue(Rounding.Down));
        }
        private static Element Node2(Element segment)
        {
            return Element.Part<V_RoundToInteger>(Element.Part<V_YOf>(segment), EnumData.GetEnumValue(Rounding.Down));
        }
        private static Element Node1Attribute(Element segment)
        {
            return Element.Part<V_RoundToInteger>(
                (Element.Part<V_XOf>(segment) % 1) * 100,
                EnumData.GetEnumValue(Rounding.Nearest)
            );
        }
        private static Element Node2Attribute(Element segment)
        {
            return Element.Part<V_RoundToInteger>(
                (Element.Part<V_YOf>(segment) % 1) * 100,
                EnumData.GetEnumValue(Rounding.Nearest)
            );
        }
    
        public static void Pathfind(TranslateRule context, PathfinderInfo info, Element pathResult, Element target, Element destination, Element pathAttributes)
        {
            context.Actions.AddRange(ArrayBuilder<Element>.Build(
                info.Path.SetVariable(
                    Element.Part<V_Append>(pathResult, destination),
                    target
                ),
                info.PathAttributes.SetVariable(
                    pathAttributes,
                    target
                )
            ));
        }
    }

    public class DijkstraNormal : DijkstraBase
    {
        private Element destination { get; }
        private IndexedVar finalNode;
        public IndexedVar finalPath { get; private set; } 
        public IndexedVar finalPathAttributes { get; private set; }

        public DijkstraNormal(TranslateRule context, PathMapVar pathmap, Element position, Element destination, Element attributes) : base(context, pathmap, position, attributes, false)
        {
            this.destination = destination;
        }

        override protected void Assign()
        {
            var lastNode = ClosestNodeToPosition(pathmap, destination);

            finalNode = IndexedVar.AssignInternalVarExt(context.VarCollection, null, "Dijkstra: Last", context.IsGlobal);
            finalPath = IndexedVar.AssignInternalVar   (context.VarCollection, null, "Dijkstra: Final Path", context.IsGlobal);
            finalPathAttributes = IndexedVar.AssignInternalVar   (context.VarCollection, null, "Dijkstra: Final Path Attributes", context.IsGlobal);
            context.Actions.AddRange(finalNode.SetVariable(lastNode));
            context.Actions.AddRange(finalPath.SetVariable(new V_EmptyArray()));
        }

        override protected Element LoopCondition()
        {
            return Element.Part<V_ArrayContains>(
                unvisited.GetVariable(),
                finalNode.GetVariable()
            );
        }

        override protected void GetResult()
        {
            Backtrack(finalNode.GetVariable(), finalPath, finalPathAttributes);
        }

        override protected void Reset()
        {
            context.Actions.AddRange(ArrayBuilder<Element>.Build(
                finalNode.SetVariable(-1)
            ));
        }
    }
    
    public class DijkstraMultiSource : DijkstraBase
    {
        private PathfinderInfo pathfinderInfo { get; }
        private Element players { get; }
        private IndexedVar closestNodesToPlayers;

        public DijkstraMultiSource(TranslateRule context, PathfinderInfo pathfinderInfo, PathMapVar pathmap, Element players, Element destination, Element attributes) : base(context, pathmap, destination, attributes, true)
        {
            this.pathfinderInfo = pathfinderInfo;
            this.players = players;
        }

        override protected void Assign()
        {
            closestNodesToPlayers = IndexedVar.AssignInternalVar(context.VarCollection, null, "Dijkstra: Closest nodes", context.IsGlobal);
            context.Actions.AddRange(closestNodesToPlayers.SetVariable(Element.Part<V_EmptyArray>()));

            ForEachBuilder getClosestNodes = new ForEachBuilder(context, players, "Get closest nodes");
            getClosestNodes.Setup();

            getClosestNodes.AddActions(
                closestNodesToPlayers.SetVariable(
                    Element.Part<V_Append>(
                        closestNodesToPlayers.GetVariable(),
                        ClosestNodeToPosition(pathmap, getClosestNodes.IndexValue)
                    )
                )
            );

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
            ForEachBuilder assignPlayerPaths = new ForEachBuilder(context, players, "Assign Player Paths");
            assignPlayerPaths.Setup();

            IndexedVar finalPath = IndexedVar.AssignInternalVar(context.VarCollection, null, "Dijkstra: Final Path", context.IsGlobal);
            IndexedVar finalPathAttributes = IndexedVar.AssignInternalVar(context.VarCollection, null, "Dijkstra: Final Path Attributes", context.IsGlobal);

            Backtrack(
                Element.Part<V_ValueInArray>(
                    closestNodesToPlayers.GetVariable(),
                    assignPlayerPaths.Index
                ),
                finalPath,
                finalPathAttributes
            );
            Pathfind(context, pathfinderInfo, finalPath.GetVariable(), assignPlayerPaths.IndexValue, position, finalPathAttributes.GetVariable());
            assignPlayerPaths.Finish();
        }

        override protected void Reset()
        {
            context.Actions.AddRange(ArrayBuilder<Element>.Build(
                closestNodesToPlayers.SetVariable(-1)
            ));
        }
    }
}