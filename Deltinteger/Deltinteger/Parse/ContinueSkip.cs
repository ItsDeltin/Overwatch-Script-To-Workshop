using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ContinueSkip
    {
        private TranslateRule Rule { get; }
        private bool IsSetup;
        private IndexReference SkipCount { get; set; }
        private IndexReference TempHolder { get; set; }
        private SkipStartMarker Skipper { get; set; }

        public ContinueSkip(TranslateRule rule)
        {
            Rule = rule;
        }

        public void Setup(ActionSet actionSet)
        {
            if (IsSetup) return;
            IsSetup = true;

            SkipCount = Rule.DeltinScript.VarCollection.Assign("continueSkip", Rule.IsGlobal, true);
            TempHolder = Rule.DeltinScript.VarCollection.Assign("continueSkipTemp", Rule.IsGlobal, true);

            A_SkipIf skipAction = Element.Part<A_SkipIf>
            (
                // Condition
                Element.Part<V_Compare>(SkipCount.GetVariable(), EnumData.GetEnumValue(Operators.Equal), new V_Number(0)),
                // Number of actions
                new V_Number(3)
            );

            Skipper = new SkipStartMarker(actionSet);
            Skipper.SkipCount = TempHolder.GetVariable();

            IActionList[] actions = ArrayBuilder<IActionList>.Build(
                new ALAction(A_Wait.MinimumWait),
                new ALAction(skipAction),
                new ALAction(TempHolder.SetVariable((Element)SkipCount.GetVariable())[0]),
                new ALAction(SkipCount.SetVariable(0)[0]),
                Skipper
            );

            Rule.Actions.InsertRange(0, actions);
        }

        public void SetSkipCount(ActionSet actionSet, Element skipCount)
        {
            CheckSetup();
            actionSet.AddAction(SkipCount.SetVariable(skipCount));
        }

        public void SetSkipCount(ActionSet actionSet, SkipEndMarker endMarker)
        {
            SetSkipCount(actionSet, GetSkipCount(endMarker));
        }

        public void ResetSkipCount(ActionSet actionSet)
        {
            SetSkipCount(actionSet, 0);
        }

        public V_Number GetSkipCount(SkipEndMarker endMarker)
        {
            CheckSetup();
            return Skipper.GetSkipCount(endMarker);
        }

        private void CheckSetup()
        {
            if (!IsSetup) throw new Exception("ContinueSkip not set up.");
        }
    }
}