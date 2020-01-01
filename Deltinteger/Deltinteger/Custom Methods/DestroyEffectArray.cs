using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("DestroyEffectArray", "Destroys an array of effects.", CustomMethodType.Action)]
    class DestroyEffectArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("effectArray", "The array of effects."),
                new ConstNumberParameter("destroyPerLoop", "The number of effects to destroy per iteration.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Element effectArray = (Element)parameterValues[0];
            double destroyPerLoop = (double)additionalParameterData[1];

            ForeachBuilder foreachBuilder = new ForeachBuilder(actionSet, effectArray);
            foreachBuilder.Setup();

            for (int i = 0; i < destroyPerLoop; i++)
            {
                if (i == 0)
                    actionSet.AddAction(
                        Element.Part<A_DestroyEffect>(foreachBuilder.IndexValue)
                    );
                else
                    actionSet.AddAction(
                        Element.Part<A_DestroyEffect>(Element.Part<V_ValueInArray>(effectArray, foreachBuilder.Index + i))
                    );
            }

            foreachBuilder.Finish();

            return null;
        }
    }

    [CustomMethod("DestroyHudArray", "Destroys an array of huds.", CustomMethodType.Action)]
    class DestroyHudArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("hudArray", "The array of huds."),
                new ConstNumberParameter("destroyPerLoop", "The number of huds to destroy per iteration.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Element hudArray = (Element)parameterValues[0];
            double destroyPerLoop = (double)additionalParameterData[1];

            ForeachBuilder foreachBuilder = new ForeachBuilder(actionSet, hudArray);
            foreachBuilder.Setup();

            for (int i = 0; i < destroyPerLoop; i++)
            {
                if (i == 0)
                    actionSet.AddAction(
                        Element.Part<A_DestroyHudText>(foreachBuilder.IndexValue)
                    );
                else
                    actionSet.AddAction(
                        Element.Part<A_DestroyHudText>(Element.Part<V_ValueInArray>(hudArray, foreachBuilder.Index + i))
                    );
            }

            foreachBuilder.Finish();

            return null;
        }
    }
}