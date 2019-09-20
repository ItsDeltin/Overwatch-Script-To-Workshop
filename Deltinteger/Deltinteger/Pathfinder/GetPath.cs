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

            //IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances", context.IsGlobal, null);
            IndexedVar distances = context.VarCollection.AssignVar(null, "Dijkstra: Distances", context.IsGlobal, Variable.J, new int[0], null);
            SetInitialDistances(context, pathmap.PathMap, distances, current.GetVariable());

            IndexedVar visited = context.VarCollection.AssignVar(null, "Dijkstra: Visited", context.IsGlobal, null);
            context.Actions.AddRange(visited.SetVariable(new V_EmptyArray()));

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
    }
}