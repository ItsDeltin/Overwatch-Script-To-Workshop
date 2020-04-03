using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using System.Linq;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("GenerateForeachArray", "Generates an array of a length where each value is equal to its index. Good for using with filtered and sorted arrays.", CustomMethodType.MultiAction_Value)]
    class GenerateForeachArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[]
        {
            new CodeParameter("Length", "The desired length of the array.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            if (parameterValues[0] is V_Number n)
            {
                V_Number[] indexes = new V_Number[n.Value < 0 ? 0 : (int)n.Value];
                for (int i = 0; i < indexes.Length; i++)
                    indexes[i] = new V_Number(i);
                return Element.CreateArray(indexes);
            }
            else
            {
                IndexReference array = actionSet.VarCollection.Assign("_foreachArrayBuilder", actionSet.IsGlobal, false);
                IndexReference length = actionSet.VarCollection.Assign("_foreachArrayBuilderLength", actionSet.IsGlobal, true);
                IndexReference i = actionSet.VarCollection.Assign("_foreachArrayBuilderIndex", actionSet.IsGlobal, true);

                actionSet.AddAction(ArrayBuilder<Element>.Build(
                    length.SetVariable((Element)parameterValues[0]),
                    array.SetVariable(new V_EmptyArray()),
                    i.SetVariable(0),
                    Element.Part<A_While>((Element)i.GetVariable() < (Element)length.GetVariable()),
                        array.SetVariable((Element)i.GetVariable(), null, (Element)i.GetVariable()),
                        i.ModifyVariable(Operation.Add, 1),
                    new A_End()
                ));

                return array.GetVariable();
            }
        }
    }
}
