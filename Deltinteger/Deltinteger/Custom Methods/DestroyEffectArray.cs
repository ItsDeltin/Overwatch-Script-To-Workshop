using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("DestroyEffectArray", CustomMethodType.Action)]
    [Parameter("Effect Array", ValueType.Any, null)]
    [ConstantParameter("Destroy Per Loop", typeof(double), 1.0)]
    class DestroyEffectArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element effectArray = (Element)Parameters[0];
            int destroyPerLoop = 1;
            if (Parameters[1] != null)
                destroyPerLoop = (int)(double)((ConstantObject)Parameters[1]).Value;

            List<Element> actions = new List<Element>();

            IndexedVar index = TranslateContext.VarCollection.AssignVar(Scope, "DestroyEffectArray index", TranslateContext.IsGlobal, null);
            actions.AddRange(index.SetVariable(new V_Number(0)));

            Element[] destroyActions = new Element[destroyPerLoop];
            for (int i = 0; i < destroyPerLoop; i++)
            {
                if (i == 0)
                    destroyActions[i] = Element.Part<A_DestroyEffect>(Element.Part<V_ValueInArray>(effectArray, index.GetVariable()));
                else
                    destroyActions[i] = Element.Part<A_DestroyEffect>(Element.Part<V_ValueInArray>(effectArray, Element.Part<V_Add>(index.GetVariable(), new V_Number(i))));
            }

            actions.AddRange(
                Element.While(
                    TranslateContext.ContinueSkip, 
                    new V_Compare(
                        index.GetVariable(), 
                        Operators.LessThan, 
                        Element.Part<V_CountOf>(effectArray)
                    ),
                    ArrayBuilder<Element>.Build
                    (
                        destroyActions,
                        index.SetVariable(Element.Part<V_Add>(index.GetVariable(), new V_Number(destroyPerLoop)))
                    )
                )
            );

            return new MethodResult(actions.ToArray(), null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Destroys an array of effects.",
                // Parameters
                "The array of effects.",
                "The number of effects to destroy per iteration."
            );
        }
    }
}