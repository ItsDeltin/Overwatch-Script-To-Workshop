using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class BakemapClass : ClassType
    {
        public ObjectVariable NodeBake { get; private set; }
        public ObjectVariable Pathmap { get; private set; }

        public BakemapClass() : base("Bakemap")
        {
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            NodeBake = AddObjectVariable(new InternalVar("NodeBake"));
            Pathmap = AddObjectVariable(new InternalVar("Pathmap"));

            serveObjectScope.AddNativeMethod(Pathfind);
        }

        private FuncMethod Pathfind => new FuncMethodBuilder() {
            Name = "Pathfind",
            Documentation = "Pathfinds specified players to the destination.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players to pathfind."),
                new CodeParameter("destination", "The position to pathfind to.")
            },
            Action = (actionSet, call) =>
            {
                // Get the ResolveInfoComponent.
                ResolveInfoComponent resolveInfo = actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();

                // Get the Pathmap class.
                PathmapClass pathmapClass = actionSet.Translate.DeltinScript.Types.GetInstance<PathmapClass>();

                Element destination = call.Get(1);
                Element nodeArray = pathmapClass.Nodes.Get()[Pathmap.Get(actionSet)];

                // Get the node closest to the destination.
                Element targetNode = Element.Part<V_IndexOfArrayValue>(
                    nodeArray,
                    Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
                        // Sort non-null nodes
                        /*Element.Part<V_FilteredArray>(nodeArray, new V_ArrayElement()),*/
                        nodeArray,
                        // Sort by distance to destination
                        Element.Part<V_DistanceBetween>(new V_ArrayElement(), destination)
                    ))
                );

                // ! debug: save targetNode
                var dbTarget = actionSet.VarCollection.Assign("dbTarget", actionSet.IsGlobal, false);
                dbTarget.Set(actionSet, targetNode);
                actionSet.AddAction(Element.Part<A_SmallMessage>(new V_AllPlayers(), dbTarget.Get()));

                // For each of the players, get the current.
                resolveInfo.Pathfind(actionSet, call.Get(0), Pathmap.Get(actionSet), NodeBake.Get(actionSet)[dbTarget.Get()], destination);
                return null;
            }
        };
    }
}