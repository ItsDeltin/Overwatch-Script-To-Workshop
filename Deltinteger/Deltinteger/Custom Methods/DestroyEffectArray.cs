using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("DestroyEffectArray", "Destroys an array of effects.", CustomMethodType.Action, typeof(NullType))]
    class DestroyEffectArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("effectArray", "The array of effects."),
                new ConstNumberParameter("destroyPerLoop", "The number of effects to destroy per iteration.", 1)
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Element effectArray = (Element)parameterValues[0];
            double destroyPerLoop = (double)additionalParameterData[1];

            actionSet.AddAction(Element.While(Element.Compare(effectArray, Operator.NotEqual, Element.EmptyArray())));

            for (int i = 0; i < destroyPerLoop; i++)
                actionSet.AddAction(
                    Element.Part("Destroy Effect", Element.FirstOf(effectArray))
                );

            actionSet.AddAction(Element.End());

            return null;
        }
    }
}
