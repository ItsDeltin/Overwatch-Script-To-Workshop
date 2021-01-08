using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using static Deltin.Deltinteger.Elements.Element;

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
                sortArray = Filter(nodes, Compare(ArrayElement(), Operator.NotEqual, Null()));

            return IndexOfArrayValue(
                nodes,
                FirstOf(Sort(
                    sortArray,
                    DistanceBetween(position, ArrayElement())
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