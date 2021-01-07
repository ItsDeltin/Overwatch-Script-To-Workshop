using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;

namespace Deltin.Deltinteger.Pathfinder
{
    public interface INodeFromPosition
    {
        Element NodeFromPosition(Element position);
    }

    class ClosestNodeFromPosition : INodeFromPosition
    {
        private readonly ActionSet _actionSet;
        private readonly PathmapClass _pathmapClass;
        private readonly Element _pathmapObject;

        public ClosestNodeFromPosition(ActionSet actionSet, PathmapClass pathmapClass, Element pathmapObject)
        {
            _actionSet = actionSet;
            _pathmapClass = pathmapClass;
            _pathmapObject = pathmapObject;
        }

        public Element NodeFromPosition(Element position)
        {
            Element nodes = _pathmapClass.Nodes.Get()[_pathmapObject], sortArray = nodes;

            // If nodes can be null, filter out the null nodes.
            if (_actionSet.DeltinScript.GetComponent<ResolveInfoComponent>().PotentiallyNullNodes)
                sortArray = Element.Part<V_FilteredArray>(nodes, new V_Compare(new V_ArrayElement(), Operators.NotEqual, new V_Null()));

            return Element.Part<V_IndexOfArrayValue>(
                nodes,
                Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(
                    sortArray,
                    Element.Part<V_DistanceBetween>(position, new V_ArrayElement())
                ))
            );
        }
    }

    class NodeFromInvocable : INodeFromPosition
    {
        private readonly ActionSet _actionSet;
        private readonly PathmapClass _pathmapClass;
        private readonly Element _pathmapObject;
        private readonly ILambdaInvocable _invocable;

        public NodeFromInvocable(ActionSet actionSet, PathmapClass pathmapClass, Element pathmapObject, ILambdaInvocable invocable)
        {
            _actionSet = actionSet;
            _pathmapClass = pathmapClass;
            _pathmapObject = pathmapObject;
            _invocable = invocable;
        }

        public Element NodeFromPosition(Element position) =>
            (Element)_invocable.Invoke(_actionSet, _pathmapClass.Nodes.Get()[_pathmapObject], position);
    }
}