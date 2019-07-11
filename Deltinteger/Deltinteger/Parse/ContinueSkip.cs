using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class ContinueSkip
    {
        private readonly bool IsGlobal;
        private readonly List<Element> Actions;
        private readonly VarCollection VarCollection;

        private IndexedVar SkipCount;

        private bool IsSetup = false;

        public ContinueSkip(bool isGlobal, List<Element> actions, VarCollection varCollection)
        {
            IsGlobal = isGlobal;
            Actions = actions;
            VarCollection = varCollection;
        }

        public void Setup()
        {
            if (IsSetup)
                return;
            IsSetup = true;

            SkipCount = VarCollection.AssignVar(null, "ContinueSkip", IsGlobal);
            if (SkipCount is RecursiveVar)
                throw new Exception();
            
            A_Wait waitAction = A_Wait.MinimumWait;
            waitAction.Comment = "ContinueSkip Wait";
            // Add the required wait
            Actions.Insert(0, waitAction);

            A_SkipIf skipAction = Element.Part<A_SkipIf>
            (
                Element.Part<V_Compare>(SkipCount.GetVariable(), EnumData.GetEnumValue(Operators.NotEqual), new V_Number(0)),
                SkipCount.GetVariable()
            );
            skipAction.Comment = "ContinueSkip Skipper";

            // Add the skip-if
            Actions.Insert(1, skipAction);
        }

        public void SetSkipCount(int number)
        {
            CheckSetup();
            Actions.AddRange(SkipCount.SetVariable(new V_Number(number)));
        }

        public void SetSkipCount(Element element)
        {
            CheckSetup();
            Actions.AddRange(SkipCount.SetVariable(element));
        }

        public void ResetSkip()
        {
            CheckSetup();
            SetSkipCount(0);
        }

        public int GetSkipCount()
        {
            // Gets the skip count based on the number of actions and the position of the coninue skip's skip-if.
            // This will need to be changed if any other components are added that insert actions into the ruleset.
            return Actions.Count - (IsSetup ? 2 : 0);
        }

        private void CheckSetup()
        {
            if (!IsSetup)
                throw new Exception("ContinueSkip not set up.");
        }
    }
}