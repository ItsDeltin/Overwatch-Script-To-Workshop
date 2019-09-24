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
            Element position               = (Element)Parameters[1];
            Element destination            = (Element)Parameters[2];

            IndexedVar finalPath = Get(TranslateContext, pathmap, position, destination);
            return new MethodResult(null, finalPath.GetVariable());
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

        public static IndexedVar Get(TranslateRule context, PathMapVar pathmap, Element position, Element destination)
        {
            var firstNode = ClosestNodeToPosition(pathmap, position);
            var lastNode = ClosestNodeToPosition(pathmap, destination);

            IndexedVar finalNode = context.VarCollection.AssignVar(null, "Dijkstra: Last",                       context.IsGlobal, Variable.D, new int[0], null);
            context.Actions.AddRange(finalNode.SetVariable(lastNode));

            IndexedVar current = context.VarCollection.AssignVar(null, "Dijkstra: Current",                      context.IsGlobal, Variable.E, new int[0], null);
            context.Actions.AddRange(current.SetVariable(firstNode));

            IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances",                  context.IsGlobal, Variable.F, new int[0], null);
            distances.Optimize2ndDim = false;
            SetInitialDistances(context, pathmap.PathMap, distances, current.GetVariable());

            IndexedVar unvisited = context.VarCollection.AssignVar(null, "Dijkstra: Visited",                    context.IsGlobal, Variable.G, new int[0], null);
            SetInitialUnvisited(context, pathmap.PathMap, unvisited);

            IndexedVar connectedSegments = context.VarCollection.AssignVar(null, "Dijkstra: Connected Segments", context.IsGlobal, Variable.H, new int[0], null);

            IndexedVar neighborIndex = context.VarCollection.AssignVar(null, "Dijkstra: Neighbor Index",         context.IsGlobal, Variable.I, new int[0], null);

            IndexedVar neighborDistance = context.VarCollection.AssignVar(null, "Dijkstra: Distance",            context.IsGlobal, Variable.J, new int[0], null);

            IndexedVar prev = context.VarCollection.AssignVar(null, "Dijkstra: Prev Array",                      context.IsGlobal, Variable.K, new int[0], null);
            prev.Optimize2ndDim = false;
            SetInitialParents(context, pathmap.PathMap, prev);

            IndexedVar finalPath = context.VarCollection.AssignVar(null, "Dijkstra: Final Path",                 context.IsGlobal, Variable.L, new int[0], null);
            context.Actions.AddRange(finalPath.SetVariable(new V_EmptyArray()));

            WhileBuilder whileBuilder = new WhileBuilder(context, 
                Element.Part<V_ArrayContains>(
                    unvisited.GetVariable(),
                    finalNode.GetVariable()
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
            forBuilder.AddActions(prev.SetVariable(current.GetVariable() + 1, null, neighborIndex.GetVariable()));

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
                current.SetVariable(finalNode.GetVariable())
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
                current.SetVariable(Element.Part<V_ValueInArray>(prev.GetVariable(), current.GetVariable()) - 1)
            ));
            backtrack.Finish();

            context.Actions.AddRange(ArrayBuilder<Element>.Build(
                finalNode.SetVariable(-1),
                current.SetVariable(-1),
                distances.SetVariable(-1),
                unvisited.SetVariable(-1),
                connectedSegments.SetVariable(-1),
                neighborIndex.SetVariable(-1),
                neighborDistance.SetVariable(-1),
                prev.SetVariable(-1)
            ));

            return finalPath;
        }

        private static Element ClosestNodeToPosition(PathMapVar pathmap, Element position)
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
            return;
            Element[] parents = new Element[pathmap.Nodes.Length];
            for (int i = 0; i < parents.Length; i++)
                parents[i] = new V_Number(-1);
            
            context.Actions.AddRange(parentVar.SetVariable(Element.CreateArray(parents)));
        }

        private static Element GetConnectedSegments(Element nodes, Element segments, Element currentIndex)
        {
            Element currentSegmentCheck = new V_ArrayElement();

            return Element.Part<V_FilteredArray>(
                segments,
                Element.Part<V_And>(
                    Element.Part<V_ArrayContains>(
                        Nodes(currentSegmentCheck),
                        currentIndex
                    ),
                    Element.Part<V_Or>(
                        new V_Compare(Attribute(currentSegmentCheck), Operators.NotEqual, new V_Number(1)),
                        new V_Compare(Node1(currentSegmentCheck), Operators.Equal, currentIndex)
                    )
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
        private static Element Attribute(Element segment)
        {
            return Element.Part<V_ZOf>(segment);
        }
    }

    [CustomMethod("Pathfind", CustomMethodType.Action)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    [VarRefParameter("Path Map")]
    [Parameter("Destination", Elements.ValueType.Vector, null)]
    class Pathfind : CustomMethodBase
    {
        override protected MethodResult Get()
        {
            if (((VarRef)Parameters[1]).Var is PathMapVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[1]).Var.Name, VarType.PathMap, ParameterLocations[1]);
            
            if (TranslateContext.ParserData.PathfinderInfo == null)
                TranslateContext.ParserData.PathfinderInfo = new PathfinderInfo(TranslateContext.ParserData);
            PathfinderInfo pathfinderInfo = TranslateContext.ParserData.PathfinderInfo;
            
            Element player                 = (Element)Parameters[0];
            PathMapVar pathmap = (PathMapVar)((VarRef)Parameters[1]).Var;

            IndexedVar destinationVar = TranslateContext.VarCollection.AssignVar(Scope, "Destination", TranslateContext.IsGlobal, null);
            TranslateContext.Actions.AddRange(
                destinationVar.SetVariable((Element)Parameters[2])
            );

            IndexedVar path = GetPath.Get(TranslateContext, pathmap, Element.Part<V_PositionOf>(player), destinationVar.GetVariable());

            TranslateContext.Actions.AddRange(
                pathfinderInfo.Nodes.SetVariable(
                    Element.Part<V_Append>(
                        pathmap.Nodes.GetVariable(),
                        destinationVar.GetVariable()
                    ),
                    player
                )
            );
            TranslateContext.Actions.AddRange(
                pathfinderInfo.Path.SetVariable(Element.Part<V_Append>(path.GetVariable(), new V_Number(pathmap.PathMap.Nodes.Length)), player)
            );

            return new MethodResult(null, null);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Moves a player to the specified position by pathfinding.",
                "The player to move.",
                "The path map.",
                "The dstination to move the player to."
            );
        }
    }
}