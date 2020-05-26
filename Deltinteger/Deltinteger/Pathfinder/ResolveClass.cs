using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathResolveClass : ClassType
    {
        public ObjectVariable ParentArray { get; private set; }
        public ObjectVariable ParentAttributeArray { get; private set; }
        public ObjectVariable Pathmap { get; private set; }

        public PathResolveClass() : base("PathResolve")
        {

        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            // Set ParentArray
            ParentArray = AddObjectVariable(new InternalVar("ParentArray"));

            // Set ParentAttributeArray
            ParentAttributeArray = AddObjectVariable(new InternalVar("ParentArray"));
            
            // Set Pathmap
            Pathmap = AddObjectVariable(new InternalVar("Pathmap"));
        }

        private FuncMethod PathfindFunction() => new FuncMethod(new FuncMethodBuilder() {
            Name = "Pathfind",
            Documentation = "Pathfinds the specified players.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players to pathfind.")
            },
            Action = (actionSet, call) => {
                // Get the ResolveInfoComponent.
                ResolveInfoComponent resolveInfo = actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();

                // Set the target player's current pathfind resolve.
                actionSet.AddAction(resolveInfo.Resolve.SetVariable(
                    value: (Element)actionSet.CurrentObject,
                    targetPlayer: (Element)call.ParameterValues[0]
                ));

                // For each of the players, get the current.
                resolveInfo.SetCurrent(actionSet, (Element)call.ParameterValues[0]);

                return null;
            }
        });
    }

    class ResolveInfoComponent : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        public IndexReference Resolve { get; private set; }
        public IndexReference DoGetCurrent { get; private set; }
        public IndexReference Current { get; private set; }
        private PathResolveClass PathResolveInstance;
        private PathmapClass PathmapInstance;

        public void Init()
        {
            Resolve = DeltinScript.VarCollection.Assign("pathfinderResolveInstance", false, true);
            DoGetCurrent = DeltinScript.VarCollection.Assign("pathfinderDoGetCurrent", false, true);
            Current = DeltinScript.VarCollection.Assign("pathfinderCurrent", false, true);

            // Get the PathResolve instance and the Pathmap instance.
            PathResolveInstance = DeltinScript.Types.GetInstance<PathResolveClass>();
            PathmapInstance = DeltinScript.Types.GetInstance<PathmapClass>();

            // Get the resolve subroutine.
            GetResolveRoutine();
        }

        private void GetResolveRoutine()
        {
            TranslateRule getResolveRule = new TranslateRule(DeltinScript, "Pathfinder: Resolve Current", RuleEvent.OngoingPlayer);
            getResolveRule.Conditions.Add(new Condition((Element)DoGetCurrent.GetVariable(), Operators.Equal, new V_True()));
            getResolveRule.ActionSet.AddAction(Current.SetVariable(ClosestNode(PlayerPosition())));
            getResolveRule.ActionSet.AddAction(DoGetCurrent.SetVariable(new V_False()));

            DeltinScript.WorkshopRules.Add(getResolveRule.GetRule());

            TranslateRule next = new TranslateRule(DeltinScript, "Pathfinder: Resolve Next", RuleEvent.OngoingPlayer);
            next.Conditions.Add(new Condition(
                Element.Part<V_DistanceBetween>(
                    PlayerPosition(),
                    CurrentPosition()
                ),
                Operators.LessThanOrEqual,
                new V_Number(PathfinderInfo.MoveToNext)
            ));
            // Set current as the current's parent.
            next.ActionSet.AddAction(Current.SetVariable(PathResolveInstance.ParentArray.Get((Element)Resolve.GetVariable())[(Element)Current.GetVariable()]));
            DeltinScript.WorkshopRules.Add(next.GetRule());
        }

        public void SetCurrent(ActionSet actionSet, Element players)
        {
            actionSet.AddAction(DoGetCurrent.SetVariable(value: new V_True(), targetPlayer: players));
        }

        public Element ClosestNode(Element position)
        {
            // Get the pathmap reference from the resolve.
            Element pathmap = PathResolveInstance.Pathmap.Get((Element)Resolve.GetVariable());

            // Get the nodes in the pathmap
            Element nodes = Element.Part<V_ValueInArray>(PathmapInstance.Nodes.GetVariable(), pathmap);

            // Get the closest node index.
            return DijkstraBase.ClosestNodeToPosition(nodes, position);
        }

        public Element CurrentPosition() => Element.Part<V_ValueInArray>(PathmapInstance.Nodes.GetVariable(), PathResolveInstance.Pathmap.Get((Element)Resolve.GetVariable()))[(Element)Current.GetVariable()];
        private Element PlayerPosition() => Element.Part<V_PositionOf>(Element.Part<V_EventPlayer>());
    }
}