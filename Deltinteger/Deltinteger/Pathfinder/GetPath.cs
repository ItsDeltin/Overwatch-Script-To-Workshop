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

        public static MethodResult Get(TranslateRule context, PathMapVar pathmap, Element position, Element destination)
        {
            var firstNode = ClosestToPosition(pathmap, position);
            var lastNode = ClosestToPosition(pathmap, destination);

            List<Element> actions = new List<Element>();

            IndexedVar current = context.VarCollection.AssignVar(null, "Dijkstra: Current", context.IsGlobal, null);
            context.Actions.AddRange(current.SetVariable(firstNode));

            IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances", context.IsGlobal, Variable.J, new int[0], null);
            //IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances", context.IsGlobal, null);
            SetInitialDistances(context, pathmap.PathMap, distances, current.GetVariable());

            IndexedVar visited = context.VarCollection.AssignVar(null, "Dijkstra: Visited", context.IsGlobal, Variable.K, new int[0], null);
            //IndexedVar visited = context.VarCollection.AssignVar(null, "Dijkstra: Visited", context.IsGlobal, null);
            context.Actions.AddRange(visited.SetVariable(new V_EmptyArray()));

            IndexedVar connectedSegments = context.VarCollection.AssignVar(null, "Dijkstra: Connected Segments", context.IsGlobal, Variable.L, new int[0], null);

            IndexedVar neighborIndex = context.VarCollection.AssignVar(null, "Dijkstra: Neighbor Index", context.IsGlobal, Variable.M, new int[0], null);

            IndexedVar neighborDistance = context.VarCollection.AssignVar(null, "Dijkstra: Distance", context.IsGlobal, Variable.N, new int[0], null);
            //IndexedVar neighbors = context.VarCollection.AssignVar(null, "Dijkstra: Neighbors", context.IsGlobal, null);
            //IndexedVar neighborDistance = context.VarCollection.AssignVar(null, "Dijkstra: Distance", context.IsGlobal, null);

            WhileBuilder whileBuilder = new WhileBuilder(context, new V_True());
            whileBuilder.Setup();

            // Get neighboring indexes
            whileBuilder.AddActions(
                connectedSegments.SetVariable(GetConnectedSegments(
                    pathmap.Nodes.GetVariable(),
                    pathmap.Segments.GetVariable(),
                    visited.GetVariable(),
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
                ),

                A_Wait.MinimumWait,
                Element.Part<A_SmallMessage>(new V_EventPlayer(), forBuilder.IndexValue), // ! Debug
                Element.Part<A_SmallMessage>(new V_EventPlayer(), neighborIndex.GetVariable()), // ! Debug
                Element.Part<A_SmallMessage>(new V_EventPlayer(), neighborDistance.GetVariable()), // ! Debug
                Element.Part<A_SmallMessage>(new V_EventPlayer(), Element.Part<V_ValueInArray>(distances.GetVariable(), neighborIndex.GetVariable())), // ! Debug
                A_Wait.MinimumWait,

                // Set the current neighbor's distance if the new distance is less than what it is now.
                distances.SetVariable(Element.TernaryConditional(
                    new V_Compare(
                        neighborDistance.GetVariable(),
                        Operators.LessThan,
                        Element.Part<V_ValueInArray>(distances.GetVariable(), neighborIndex.GetVariable())
                    ),
                    neighborDistance.GetVariable(),
                    Element.Part<V_ValueInArray>(distances.GetVariable(), neighborIndex.GetVariable())
                ), null, neighborIndex.GetVariable())
            ));
            forBuilder.Finish();

            whileBuilder.AddActions(ArrayBuilder<Element>.Build(
                // Add the current to the visited array.
                visited.SetVariable(Element.Part<V_Append>(visited.GetVariable(), current.GetVariable())),
                Element.Part<A_SmallMessage>(new V_EventPlayer(), new V_String(null, "finished"))
            ));

            whileBuilder.Finish();

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
                distances[i] = new V_Number(9999);
            
            context.Actions.AddRange(distancesVar.SetVariable(Element.CreateArray(distances)));
            context.Actions.AddRange(distancesVar.SetVariable(new V_Number(0), null, currentIndex));
        }

        private static Element GetConnectedSegments(Element nodes, Element segments, Element visited, Element currentIndex)
        {
            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_ArrayContains>(
                    Nodes(new V_ArrayElement()),
                    currentIndex
                )
            );
            /*
            return Element.Part<V_FilteredArray>(
                nodes,
                Element.Part<V_And>(
                    Element.Part<V_IsTrueForAny>(
                        segments,
                        Element.Part<V_ArrayContains>(
                            Nodes(new V_ArrayElement()),
                            currentIndex
                        )
                    ),
                    Element.Part<V_Not>(Element.Part<V_ArrayContains>(
                        visited,
                        new V_ArrayElement()
                    ))
                )
            );
            */
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