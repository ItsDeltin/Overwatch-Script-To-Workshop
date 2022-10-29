using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    abstract class PathLookahead
    {
        protected IndexReference Look { get; private set; }
        protected ActionSet ActionSet { get; private set; }
        protected ResolveInfoComponent ResolveInfo { get; private set; }
        protected Element Target { get; private set; }
        protected PathmapClass PathmapInstance { get; private set; }

        public Element Get(ResolveInfoComponent resolveInfo, ActionSet actionSet, Element target)
        {
            ActionSet = actionSet;
            ResolveInfo = resolveInfo;
            Target = target;
            PathmapInstance = actionSet.DeltinScript.GetComponent<PathfinderTypesComponent>().Pathmap;

            // Lookahead status
            IndexReference result = actionSet.VarCollection.Assign("Lookahead: Result", actionSet.IsGlobal, true);
            actionSet.AddAction(result.SetVariable(Element.False()));

            // The lookhead controller
            Look = actionSet.VarCollection.Assign("Pathfind: Lookahead", actionSet.IsGlobal, true);
            actionSet.AddAction(Look.SetVariable(resolveInfo.Current.Get(target)));

            // The loop
            actionSet.AddAction(Element.While(Element.And(
                !result.Get(),
                LoopCondition()
            )));

            // Set the result.
            actionSet.AddAction(result.SetVariable(SetResult()));

            // End
            actionSet.AddAction(Look.SetVariable(Next));
            actionSet.AddAction(Element.End());
            return result.Get();
        }

        protected abstract Element LoopCondition();
        protected abstract Element SetResult();

        /// <summary>The next node.</summary>
        protected Element Next => ResolveInfo.ParentArray.Get(Target)[Look.Get()] - 1;
        /// <summary>The current segment resolved from the current node and the next node.</summary>
        protected Element CurrentSegment => Element.FirstOf(PathmapInstance.SegmentsFromNodes(ActionSet.ToWorkshop, ResolveInfo.PathmapReference.Get(Target), Look.Get(), Next));
        /// <summary>Determines if the node after the current node is not the last one.</summary>
        protected Element NextIsNotEnd => Element.Compare(
            Next,
            Operator.GreaterThanOrEqual,
            Element.Num(0)
        );
    }

    class IsTravelingToNode : PathLookahead
    {
        private readonly Element _node;

        public IsTravelingToNode(Element node)
        {
            _node = node;
        }

        protected override Element LoopCondition() => Element.Compare(
            Look.GetVariable(),
            Operator.GreaterThanOrEqual,
            Element.Num(0)
        );
        protected override Element SetResult() => Element.Compare(Look.Get(), Operator.Equal, _node);
    }

    class IsTravelingToSegment : PathLookahead
    {
        private readonly Element _segment;

        public IsTravelingToSegment(Element segment)
        {
            _segment = segment;
        }

        protected override Element LoopCondition() => NextIsNotEnd;
        protected override Element SetResult() => Element.Compare(
            _segment,
            Operator.Equal,
            CurrentSegment
        );
    }

    class IsTravelingToAttribute : PathLookahead
    {
        private readonly Element _attribute;

        public IsTravelingToAttribute(Element attribute)
        {
            _attribute = attribute;
        }

        protected override Element LoopCondition() => NextIsNotEnd;
        protected override Element SetResult() => Element.Any(
            // Get the attributes where the first node is the current and the second node is the next.
            Element.Filter(
                // Get the attribute array.
                PathmapInstance.Attributes.GetWithReference(ActionSet.ToWorkshop, ResolveInfo.PathmapReference.Get(Target)),
                // Filter.
                Element.And(
                    // Compare the first node to current.
                    Element.Compare(Element.XOf(Element.ArrayElement()), Operator.Equal, Look.Get()),
                    // Compare the second node to next.
                    Element.Compare(Element.YOf(Element.ArrayElement()), Operator.Equal, Next)
                )
            ),
            // Check if the current attribute is equal to the attribute being looked for.
            Element.Compare(Element.ZOf(Element.ArrayElement()), Operator.Equal, _attribute)
        );
    }
}