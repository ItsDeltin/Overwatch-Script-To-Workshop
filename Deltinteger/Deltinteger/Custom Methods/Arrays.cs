using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("SumOfArray", "Gets the sum of an array.", CustomMethodType.MultiAction_Value)]
    class SumOfArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[]
        {
            new CodeParameter("Array", "The array to sum.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            IndexReference array = actionSet.VarCollection.Assign("_arrayToSum", actionSet.IsGlobal, true);
            IndexReference sum = actionSet.VarCollection.Assign("_sumOfArray", actionSet.IsGlobal, true);

            actionSet.AddAction(array.SetVariable((Element)parameterValues[0]));
            actionSet.AddAction(sum.SetVariable(0));
            ForeachBuilder builder = new ForeachBuilder(actionSet, array.GetVariable());
            actionSet.AddAction(sum.ModifyVariable(Operation.Add, builder.IndexValue));
            builder.Finish();
            return sum.GetVariable();
        }
    }
}
