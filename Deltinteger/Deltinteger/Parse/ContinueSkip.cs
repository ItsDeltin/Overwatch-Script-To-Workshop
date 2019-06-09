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

        private Var SkipCount;

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

            SkipCount = VarCollection.AssignVar(IsGlobal);
            
            // Add the required wait
            Actions.Insert(0, Element.Part<A_Wait>(new V_Number(Constants.MINIMUM_WAIT)));

            // Add the skip-if
            Actions.Insert(1, 
                Element.Part<A_SkipIf>
                (
                    Element.Part<V_Compare>(SkipCount.GetVariable(), Operators.NotEqual, new V_Number(0)),
                    SkipCount.GetVariable()
                )
            );
        }

        public void SetSkipCount(int number)
        {
            CheckSetup();
            Actions.Add(SkipCount.SetVariable(new V_Number(number)));
        }

        public void ResetSkip()
        {
            CheckSetup();
            SetSkipCount(0);
        }

        private void CheckSetup()
        {
            if (!IsSetup)
                throw new Exception("ContinueSkip not set up.");
        }
    }
}