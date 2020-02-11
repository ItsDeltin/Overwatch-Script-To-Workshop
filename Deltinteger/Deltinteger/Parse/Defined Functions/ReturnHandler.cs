using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ReturnHandler
    {
        protected readonly ActionSet ActionSet;
        private readonly bool MultiplePaths;

        // If `MultiplePaths` is true, use `ReturnStore`. Else use `ReturningValue`.
        private readonly IndexReference ReturnStore;
        private IWorkshopTree ReturningValue;

        private bool ValueWasReturned;

        private readonly List<SkipStartMarker> skips = new List<SkipStartMarker>();

        public ReturnHandler(ActionSet actionSet, string methodName, bool multiplePaths)
        {
            ActionSet = actionSet;
            MultiplePaths = multiplePaths;

            if (multiplePaths)
                ReturnStore = actionSet.VarCollection.Assign("_" + methodName + "ReturnValue", actionSet.IsGlobal, true);
        }

        public virtual void ReturnValue(IWorkshopTree value)
        {
            if (!MultiplePaths && ValueWasReturned)
                throw new Exception("_multiplePaths is set as false and 2 expressions were returned.");
            ValueWasReturned = true;

            // Multiple return paths.
            if (MultiplePaths)
                ActionSet.AddAction(ReturnStore.SetVariable((Element)value));
            // One return path.
            else
                ReturningValue = value;
        }

        public virtual void Return()
        {
            SkipStartMarker returnSkipStart = new SkipStartMarker(ActionSet);
            ActionSet.AddAction(returnSkipStart);

            // 0 skip workaround.
            // ActionSet.AddAction(new A_Abort() { Disabled = true });

            skips.Add(returnSkipStart);
        }

        public virtual void ApplyReturnSkips()
        {
            SkipEndMarker methodEndMarker = new SkipEndMarker();
            ActionSet.AddAction(methodEndMarker);

            foreach (var returnSkip in skips)
                returnSkip.SetEndMarker(methodEndMarker);
        }

        public virtual IWorkshopTree GetReturnedValue()
        {
            if (MultiplePaths)
                return ReturnStore.GetVariable();
            else
                return ReturningValue;
        }
    }

    public class RuleReturnHandler : ReturnHandler
    {
        public RuleReturnHandler(ActionSet actionSet) : base(actionSet, null, false) {}

        public override void ApplyReturnSkips() => throw new Exception("Can't apply return skips in a rule.");
        public override IWorkshopTree GetReturnedValue() => throw new Exception("Can't get the returned value of a rule.");
        public override void ReturnValue(IWorkshopTree value) => throw new Exception("Can't return a value in a rule..");

        public override void Return()
        {
            ActionSet.AddAction(Element.Part<A_Abort>());
        }
    }
}