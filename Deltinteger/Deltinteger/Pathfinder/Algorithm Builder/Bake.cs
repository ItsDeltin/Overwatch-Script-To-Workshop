using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapBake : IPathfinderInfo
    {
        public ActionSet ActionSet { get; }
        private readonly PathmapClass _pathmapClass;
        private readonly BakemapClass _bakemapClass;
        private readonly Element _pathmapObject;
        private readonly ILambdaInvocable _onLoop;
        private IndexReference _bakemap;
        private ForBuilder _nodeLoop;
        private PathfindAlgorithmBuilder _builder;
        private IndexReference _bakeWait;

        public PathmapBake(ActionSet actionSet, Element pathmapObject, Element attributes, ILambdaInvocable onLoop)
        {
            ActionSet = actionSet;
            _pathmapClass = actionSet.Translate.DeltinScript.Types.GetInstance<PathmapClass>();
            _bakemapClass = actionSet.Translate.DeltinScript.Types.GetInstance<BakemapClass>();
            _pathmapObject = pathmapObject;
            EnabledAttributes = attributes;
            _onLoop = onLoop;
        }

        public Element Bake(Action<Element> progress)
        {
            if (_onLoop == null)
            {
                _bakeWait = ActionSet.VarCollection.Assign("bakeWait", ActionSet.IsGlobal, false);
                _bakeWait.Set(ActionSet, 0);
            }

            // Assign bakemap then set it to an empty array.
            _bakemap = ActionSet.VarCollection.Assign("bakemap", ActionSet.IsGlobal, false);
            _bakemap.Set(ActionSet, EmptyArray());

            // Loop through each node.
            _nodeLoop = new ForBuilder(ActionSet, "bakemapNode", NodeArrayLength);
            _builder = new PathfindAlgorithmBuilder(this);
            progress(Progress);

            _nodeLoop.Init(); // Start the node loop.
            _builder.Get(); // Run pathfinder.
            _nodeLoop.End(); // End the node loop.

            // Create a new Bakemap class instance.
            var newBakemap = _bakemapClass.Create(ActionSet, ActionSet.Translate.DeltinScript.GetComponent<ClassData>());
            _bakemapClass.Pathmap.Set(ActionSet, newBakemap.Get(), _pathmapObject);
            _bakemapClass.NodeBake.Set(ActionSet, newBakemap.Get(), _bakemap.Get());
            return newBakemap.Get();
        }

        public Element Progress => Min(Num(1), (_nodeLoop.Value / NodeArrayLength)
            + ((NodeArrayLength - CountOf(_builder.Unvisited.Get()))
                / Pow(NodeArrayLength, Num(2))));

        // When the dijkstra is finished, set the bakemap's node to the parent array.
        void IPathfinderInfo.Finished() => _bakemap.Set(ActionSet, _builder.ParentArray.Get(), index: _nodeLoop.Value);

        // Wait on loop.
        void IPathfinderInfo.OnConnectLoop() {}
        void IPathfinderInfo.OnLoop()
        {
            if (_onLoop == null)
            {
                ActionSet.AddAction(_bakeWait.ModifyVariable(Operation.Add, 1));
                ActionSet.AddAction(SkipIf(_bakeWait.Get() % 6, Num(1)));
                ActionSet.AddAction(Wait());
            }
            else
                _onLoop.Invoke(ActionSet);
        }

        void IPathfinderInfo.OnLoopEnd() {}

        Element IPathfinderInfo.InitialNode => _nodeLoop.Value;
        public Element NodeArray => _pathmapClass.Nodes.Get()[_pathmapObject];
        Element NodeArrayLength => Element.CountOf(NodeArray);
        Element IPathfinderInfo.SegmentArray => _pathmapClass.Segments.Get()[_pathmapObject];
        Element IPathfinderInfo.AttributeArray => _pathmapClass.Attributes.Get()[_pathmapObject];
        Element IPathfinderInfo.LoopCondition => _builder.AnyAccessableUnvisited();
        public Element EnabledAttributes { get; }
    }
}