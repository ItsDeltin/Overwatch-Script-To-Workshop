using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Pathfinder;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    [CustomMethod("GetPath", CustomMethodType.MultiAction_Value)]
    [VarRefParameter("Path Map")]
    [Parameter("Position", Elements.ValueType.Vector, null)]
    [Parameter("Destination", Elements.ValueType.Vector, null)]
    public class GetPath : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            if (((VarRef)Parameters[0]).Var is PathMapVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[0]).Var.Name, VarType.PathMap, ParameterLocations[0]);
            
            PathMapVar pathmap = (PathMapVar)((VarRef)Parameters[0]).Var;
            Element position             = (Element)Parameters[1];
            Element destination          = (Element)Parameters[2];

            return Get(TranslateContext, pathmap, position, destination);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Gets the path to the destination.",
                // Descriptions
                "The initial position.",
                "The final destination."
            );
        }

        private static readonly V_Number Infinity = new V_Number(9999);

        public static MethodResult Get(TranslateRule context, PathMapVar pathmap, Element position, Element destination)
        {
            var firstNode = ClosestToPosition(pathmap, position);
            var lastNode = ClosestToPosition(pathmap, destination);

            List<Element> actions = new List<Element>();

            IndexedVar current = context.VarCollection.AssignVar(null, "Dijkstra: Current", context.IsGlobal, Variable.I, new int[0], null);
            //IndexedVar current = context.VarCollection.AssignVar(null, "Dijkstra: Current", context.IsGlobal, null);
            context.Actions.AddRange(current.SetVariable(firstNode));

            IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances", context.IsGlobal, Variable.J, new int[0], null);
            //IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances", context.IsGlobal, null);
            SetInitialDistances(context, pathmap.PathMap, distances, current.GetVariable());

            IndexedVar unvisited = context.VarCollection.AssignVar(null, "Dijkstra: Visited", context.IsGlobal, Variable.K, new int[0], null);
            //IndexedVar visited = context.VarCollection.AssignVar(null, "Dijkstra: Visited", context.IsGlobal, null);
            SetInitialUnvisited(context, pathmap.PathMap, unvisited);
            //context.Actions.AddRange(unvisited.SetVariable(new V_EmptyArray()));

            IndexedVar connectedSegments = context.VarCollection.AssignVar(null, "Dijkstra: Connected Segments", context.IsGlobal, Variable.L, new int[0], null);

            IndexedVar neighborIndex = context.VarCollection.AssignVar(null, "Dijkstra: Neighbor Index", context.IsGlobal, Variable.M, new int[0], null);

            IndexedVar neighborDistance = context.VarCollection.AssignVar(null, "Dijkstra: Distance", context.IsGlobal, Variable.N, new int[0], null);
            //IndexedVar neighbors = context.VarCollection.AssignVar(null, "Dijkstra: Neighbors", context.IsGlobal, null);
            //IndexedVar neighborDistance = context.VarCollection.AssignVar(null, "Dijkstra: Distance", context.IsGlobal, null);

            IndexedVar prev = context.VarCollection.AssignVar(null, "Dijkstra: Prev Array", context.IsGlobal, Variable.O, new int[0], null);
            SetInitialParents(context, pathmap.PathMap, prev);

            IndexedVar finalPath = context.VarCollection.AssignVar(null, "Dijkstra: Final Path", context.IsGlobal, Variable.P, new int[0], null);
            context.Actions.AddRange(finalPath.SetVariable(new V_EmptyArray()));

            WhileBuilder whileBuilder = new WhileBuilder(context, 
                Element.Part<V_ArrayContains>(
                    unvisited.GetVariable(),
                    lastNode
                )
            );
            whileBuilder.Setup();

            // Get neighboring indexes
            whileBuilder.AddActions(
                connectedSegments.SetVariable(GetConnectedSegments(
                    pathmap.Nodes.GetVariable(),
                    pathmap.Segments.GetVariable(),
                    current.GetVariable()
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
                    Element.Part<V_Add>(
                        Element.Part<V_DistanceBetween>(
                            Element.Part<V_ValueInArray>(pathmap.Nodes.GetVariable(), neighborIndex.GetVariable()),
                            Element.Part<V_ValueInArray>(pathmap.Nodes.GetVariable(), current.GetVariable())
                        ),
                        Element.Part<V_ValueInArray>(distances.GetVariable(), current.GetVariable())
                    )
                )
            ));

            // Set the current neighbor's distance if the new distance is less than what it is now.
            IfBuilder ifBuilder = new IfBuilder(context, new V_Compare(
                neighborDistance.GetVariable(),
                Operators.LessThan,
                Element.Part<V_ValueInArray>(distances.GetVariable(), neighborIndex.GetVariable())
            ));
            ifBuilder.Setup();

            forBuilder.AddActions(distances.SetVariable(neighborDistance.GetVariable(), null, neighborIndex.GetVariable()));
            forBuilder.AddActions(prev.SetVariable(current.GetVariable(), null, neighborIndex.GetVariable()));

            ifBuilder.Finish();
            forBuilder.Finish();

            whileBuilder.AddActions(ArrayBuilder<Element>.Build(
                // Add the current to the visited array.
                unvisited.SetVariable(Element.Part<V_RemoveFromArray>(unvisited.GetVariable(), current.GetVariable())),

                // Set the current node as the smallest unvisited.
                current.SetVariable(LowestUnvisited(pathmap.Nodes.GetVariable(), distances.GetVariable(), unvisited.GetVariable()))
            ));

            whileBuilder.Finish();

            context.Actions.AddRange(
                current.SetVariable(lastNode)
            );

            // Get the path.
            WhileBuilder backtrack = new WhileBuilder(context, new V_Compare(
                current.GetVariable(),
                Operators.NotEqual,
                new V_Number(-1)
            ));
            backtrack.Setup();
            backtrack.AddActions(ArrayBuilder<Element>.Build(
                finalPath.SetVariable(Element.Part<V_Append>(current.GetVariable(), finalPath.GetVariable())),
                current.SetVariable(Element.Part<V_ValueInArray>(prev.GetVariable(), current.GetVariable()))
            ));
            backtrack.Finish();

            return new MethodResult(null, null);
        }

        private static Element ClosestToPosition(PathMapVar pathmap, Element position)
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
            Element[] distances = new Element[pathmap.Nodes.Length];
            for (int i = 0; i < distances.Length; i++)
                distances[i] = Infinity;
            
            context.Actions.AddRange(distancesVar.SetVariable(Element.CreateArray(distances)));
            context.Actions.AddRange(distancesVar.SetVariable(new V_Number(0), null, currentIndex));
        }

        private static void SetInitialUnvisited(TranslateRule context, PathMap pathmap, IndexedVar unvisitedVar)
        {
            Element[] unvisited = new Element[pathmap.Nodes.Length];
            for (int i = 0; i < unvisited.Length; i++)
                unvisited[i] = new V_Number(i);
            
            context.Actions.AddRange(unvisitedVar.SetVariable(Element.CreateArray(unvisited)));
        }

        private static void SetInitialParents(TranslateRule context, PathMap pathmap, IndexedVar parentVar)
        {
            Element[] parents = new Element[pathmap.Nodes.Length];
            for (int i = 0; i < parents.Length; i++)
                parents[i] = new V_Number(-1);
            
            context.Actions.AddRange(parentVar.SetVariable(Element.CreateArray(parents)));
        }

        private static Element GetConnectedSegments(Element nodes, Element segments, Element currentIndex)
        {
            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_ArrayContains>(
                    Nodes(new V_ArrayElement()),
                    currentIndex
                )
            );
        }

        private static Element LowestUnvisited(Element nodes, Element distances, Element unvisited)
        {
            return Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
                unvisited,
                Element.Part<V_ValueInArray>(
                    distances,
                    new V_ArrayElement()
                )
            ));
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
    }
}