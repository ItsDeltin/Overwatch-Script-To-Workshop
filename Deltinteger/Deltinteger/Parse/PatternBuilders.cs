using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class IfBuilder
    {
        protected ActionSet ActionSet { get; }
        public IWorkshopTree Condition { get; protected set; }
        public bool WasSetup { get; private set; }
        private SkipStartMarker SkipMarker;

        public IfBuilder(ActionSet actionSet, IWorkshopTree condition)
        {
            ActionSet = actionSet;
            Condition = condition;
        }

        public void Setup()
        {
            if (WasSetup) throw new Exception("Pattern builder already set up.");
            WasSetup = true;

            SkipMarker = new SkipStartMarker(ActionSet, Condition);
            ActionSet.AddAction(SkipMarker);
        }

        public void Finish()
        {
            SkipEndMarker endMarker = new SkipEndMarker();
            ActionSet.AddAction(endMarker);
            SkipMarker.SkipCount = SkipMarker.GetSkipCount(endMarker);
        }
    }

    public class WhileBuilder
    {
        protected ActionSet ActionSet { get; }
        public bool WasSetup { get; private set; }
        // Start of the loop, using End to mark the end that the continue skip will jump to.
        private SkipEndMarker LoopStart { get; set; }
        private SkipStartMarker LoopSkipStart { get; set; }
        public IWorkshopTree Condition { get; protected set; }

        public WhileBuilder(ActionSet actionSet, IWorkshopTree condition)
        {
            ActionSet = actionSet;
            Condition = condition;
        }
        protected WhileBuilder(ActionSet actionSet)
        {
            ActionSet = actionSet;
        }

        public void Setup()
        {
            if (WasSetup) throw new Exception("Pattern builder already set up.");
            WasSetup = true;
            ActionSet.ContinueSkip.Setup(ActionSet);

            LoopStart = new SkipEndMarker();
            ActionSet.AddAction(LoopStart);

            LoopSkipStart = new SkipStartMarker(ActionSet, Condition);
            ActionSet.AddAction(LoopSkipStart);
        }

        public virtual void Finish()
        {
            if (!WasSetup) throw new Exception("Pattern builder not set up yet.");
            ActionSet.ContinueSkip.SetSkipCount(ActionSet, LoopStart);
            ActionSet.AddAction(Element.Part<A_Loop>());
            ActionSet.ContinueSkip.ResetSkipCount(ActionSet);

            SkipEndMarker loopEnd = new SkipEndMarker();
            ActionSet.AddAction(loopEnd);
            LoopSkipStart.SkipCount = LoopSkipStart.GetSkipCount(loopEnd);
        }
    }

    public class ForeachBuilder : WhileBuilder
    {
        private IndexReference IndexStore { get; }
        public IWorkshopTree Array { get; }
        public Element Index { get; }
        public Element IndexValue { get; }

        public ForeachBuilder(ActionSet actionSet, IWorkshopTree array) : base(actionSet)
        {
            IndexStore = actionSet.VarCollection.Assign("foreachIndex,", actionSet.IsGlobal, true);
            Array = array;
            Condition = new V_Compare(IndexStore.GetVariable(), Operators.LessThan, Element.Part<V_CountOf>(Array));
            Index = (Element)IndexStore.GetVariable();
            IndexValue = Element.Part<V_ValueInArray>(Array, IndexStore.GetVariable());
        }

        public override void Finish()
        {
            ActionSet.AddAction(IndexStore.ModifyVariable(Operation.Add, 1));
            base.Finish();
        }
    }
}