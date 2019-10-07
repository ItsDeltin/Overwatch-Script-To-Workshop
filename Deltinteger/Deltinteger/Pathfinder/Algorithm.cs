using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public abstract class DijkstraBase
    {
        private static readonly V_Number Infinity = new V_Number(9999);
        private static readonly V_Number LeastNot0 = new V_Number(0.001);

        protected TranslateRule context { get; }
        protected PathMapVar pathmap { get; }
        protected Element position { get; }
        private bool reversed { get; }

        protected IndexedVar unvisited { get; private set; }
        protected IndexedVar current { get; private set; }
        protected IndexedVar parentArray { get; private set; }

        public DijkstraBase(TranslateRule context, PathMapVar pathmap, Element position, bool reversed)
        {
            this.context = context;
            this.pathmap = pathmap;
            this.position = position;
            this.reversed = reversed;
        }

        public void Get()
        {
            var firstNode = ClosestNodeToPosition(pathmap, position);

            Assign();
            
            current = context.VarCollection.AssignVar(null, "Dijkstra: Current",                                 context.IsGlobal, null);
            context.Actions.AddRange(current.SetVariable(firstNode));

            IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances",                  context.IsGlobal, null);
            distances.Optimize2ndDim = false;
            SetInitialDistances(context, pathmap.PathMap, distances, current.GetVariable());

            unvisited = context.VarCollection.AssignVar(null, "Dijkstra: Unvisited",                             context.IsGlobal, null);
            SetInitialUnvisited(context, pathmap.PathMap, unvisited);

            IndexedVar connectedSegments = context.VarCollection.AssignVar(null, "Dijkstra: Connected Segments", context.IsGlobal, null);

            IndexedVar neighborIndex = context.VarCollection.AssignVar(null, "Dijkstra: Neighbor Index",         context.IsGlobal, null);

            IndexedVar neighborDistance = context.VarCollection.AssignVar(null, "Dijkstra: Distance",            context.IsGlobal, null);

            parentArray = context.VarCollection.AssignVar(null, "Dijkstra: Parent Array",                        context.IsGlobal, null);
            parentArray.Optimize2ndDim = false;
            //SetInitialParents(context, pathmap.PathMap, parentArray);

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
                        Node2(forBuilder.IndexValue)
                    )
                ),

                // Get the distance between the current and the neighbor index.
                neighborDistance.SetVariable(
                    (
                        Element.Part<V_DistanceBetween>(
                            Element.Part<V_ValueInArray>(pathmap.Nodes.GetVariable(), neighborIndex.GetVariable()),
                            Element.Part<V_ValueInArray>(pathmap.Nodes.GetVariable(), current.GetVariable())
                        ) +
                        Element.Part<V_ValueInArray>(distances.GetVariable(), current.GetVariable())
                    )
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
                parentArray.SetVariable(-1)
            ));

            Reset();
        }

        protected abstract void Assign();
        protected abstract Element LoopCondition();
        protected abstract void GetResult();
        protected abstract void Reset();

        protected void Backtrack(Element destination, IndexedVar finalPath)
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
            backtrack.AddActions(ArrayBuilder<Element>.Build(
                !reversed ? 
                    finalPath.SetVariable(Element.Part<V_Append>(current.GetVariable(), finalPath.GetVariable()))
                    :
                    finalPath.SetVariable(Element.Part<V_Append>(finalPath.GetVariable(), current.GetVariable())),
                current.SetVariable(Element.Part<V_ValueInArray>(parentArray.GetVariable(), current.GetVariable()) - 1)
            ));
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

        private static Element GetConnectedSegments(Element nodes, Element segments, Element currentIndex, bool reversed)
        {
            Element currentSegmentCheck = new V_ArrayElement();

            Element isValid;
            if (!reversed)
                isValid = Element.Part<V_Or>(
                    new V_Compare(Attribute(currentSegmentCheck), Operators.NotEqual, new V_Number(1)),
                    new V_Compare(Node1(currentSegmentCheck), Operators.Equal, currentIndex)
                );
            else
                isValid = Element.Part<V_Or>(
                    new V_Compare(Attribute(currentSegmentCheck), Operators.NotEqual, new V_Number(1)),
                    new V_Compare(Node2(currentSegmentCheck), Operators.Equal, currentIndex)
                );

            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_And>(
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
                Infinity
            );
        }

        private static Element Nodes(Element segment)
        {
            return Element.CreateArray(Node1(segment), Node2(segment));
        }
        private static Element Node1(Element segment)
        {
            return Element.Part<V_XOf>(segment);
        }
        private static Element Node2(Element segment)
        {
            return Element.Part<V_YOf>(segment);
        }
        private static Element Attribute(Element segment)
        {
            return Element.Part<V_ZOf>(segment);
        }
    
        public static void Pathfind(TranslateRule context, PathfinderInfo info, PathMapVar pathMapVar, Element pathResult, Element target, Element destination)
        {
            context.Actions.AddRange(ArrayBuilder<Element>.Build(
                info.Nodes.SetVariable(
                    Element.Part<V_Append>(
                        pathMapVar.Nodes.GetVariable(),
                        destination
                    ),
                    target
                ),
                info.Path.SetVariable(
                    Element.Part<V_Append>(pathResult, new V_Number(pathMapVar.PathMap.Nodes.Length)),
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

        public DijkstraNormal(TranslateRule context, PathMapVar pathmap, Element position, Element destination) : base(context, pathmap, position, false)
        {
            this.destination = destination;
        }

        override protected void Assign()
        {
            var lastNode = ClosestNodeToPosition(pathmap, destination);

            finalNode = context.VarCollection.AssignVar(null, "Dijkstra: Last", context.IsGlobal, null);
            context.Actions.AddRange(finalNode.SetVariable(lastNode));

            finalPath = context.VarCollection.AssignVar(null, "Dijkstra: Final Path", context.IsGlobal, null);
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
            Backtrack(finalNode.GetVariable(), finalPath);
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

        public DijkstraMultiSource(TranslateRule context, PathfinderInfo pathfinderInfo, PathMapVar pathmap, Element players, Element destination) : base(context, pathmap, destination, true)
        {
            this.pathfinderInfo = pathfinderInfo;
            this.players = players;
        }

        override protected void Assign()
        {
            closestNodesToPlayers = context.VarCollection.AssignVar(null, "Dijkstra: Closest nodes", context.IsGlobal, null);
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

            IndexedVar finalPath = context.VarCollection.AssignVar(null, "Dijkstra: Final Path", context.IsGlobal, null);

            Backtrack(
                Element.Part<V_ValueInArray>(
                    closestNodesToPlayers.GetVariable(),
                    assignPlayerPaths.Index
                ),
                finalPath
            );
            Pathfind(context, pathfinderInfo, pathmap, finalPath.GetVariable(), assignPlayerPaths.IndexValue, position);
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