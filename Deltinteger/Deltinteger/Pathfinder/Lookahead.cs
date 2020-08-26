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
            PathmapInstance = actionSet.Translate.DeltinScript.Types.GetInstance<PathmapClass>();

            // Lookahead status
            IndexReference result = actionSet.VarCollection.Assign("Lookahead: Result", actionSet.IsGlobal, true);
            actionSet.AddAction(result.SetVariable(new V_False()));

            // The lookhead controller
            Look = actionSet.VarCollection.Assign("Pathfind: Lookahead", actionSet.IsGlobal, true);
            actionSet.AddAction(Look.SetVariable(resolveInfo.Current.Get(target)));

            // The loop
            actionSet.AddAction(Element.Part<A_While>(Element.Part<V_And>(
                !result.Get(),
                LoopCondition()
            )));

            // Set the result.
            actionSet.AddAction(result.SetVariable(SetResult()));

            // End
            actionSet.AddAction(Look.SetVariable(Next));
            actionSet.AddAction(new A_End());
            return result.Get();
        }

        protected abstract Element LoopCondition();
        protected abstract Element SetResult();

        /// <summary>The next node.</summary>
        protected Element Next => ResolveInfo.ParentArray.Get(Target)[Look.Get()] - 1;
        /// <summary>The current segment resolved from the current node and the next node.</summary>
        protected Element CurrentSegment => Element.Part<V_FirstOf>(PathmapInstance.SegmentsFromNodes(ResolveInfo.PathmapReference.Get(Target), Look.Get(), Next));
        /// <summary>Determines if the node after the current node is not the last one.</summary>
        protected Element NextIsNotEnd => new V_Compare(
            Next,
            Operators.GreaterThanOrEqual,
            new V_Number(0)
        );
    }

    class IsTravelingToNode : PathLookahead
    {
        private readonly Element _node;

        public IsTravelingToNode(Element node)
        {
            _node = node;
        }

        protected override Element LoopCondition() => new V_Compare(
            Look.GetVariable(),
            Operators.GreaterThanOrEqual,
            new V_Number(0)
        );
        protected override Element SetResult() => new V_Compare(Look.Get(), Operators.Equal, _node);
    }

    class IsTravelingToSegment : PathLookahead
    {
        private readonly Element _segment;

        public IsTravelingToSegment(Element segment)
        {
            _segment = segment;
        }

        protected override Element LoopCondition() => NextIsNotEnd;
        protected override Element SetResult() => new V_Compare(
            _segment,
            Operators.Equal,
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
        protected override Element SetResult() => Element.Part<V_IsTrueForAny>(
            // Get the attributes where the first node is the current and the second node is the next.
            Element.Part<V_FilteredArray>(
                // Get the attribute array.
                PathmapInstance.Attributes.Get()[ResolveInfo.PathmapReference.Get(Target)],
                // Filter.
                Element.Part<V_And>(
                    // Compare the first node to current.
                    new V_Compare(Element.Part<V_XOf>(Element.Part<V_ArrayElement>()), Operators.Equal, Look.Get()),
                    // Compare the second node to next.
                    new V_Compare(Element.Part<V_YOf>(Element.Part<V_ArrayElement>()), Operators.Equal, Next)
                )
            ),
            // Check if the current attribute is equal to the attribute being looked for.
            new V_Compare(Element.Part<V_ZOf>(Element.Part<V_ArrayElement>()), Operators.Equal, _attribute)
        );
    }
}