using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using System.Linq;
using Deltin.Deltinteger.Models;

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
            if (parameterValues[0] is V_Array ar)
            {
                if (ar.ParameterValues.All(element => element is V_Number))
                    return new V_Number(ar.ParameterValues.Sum(element => ((V_Number)element).Value));
                else if (ar.ParameterValues.All(element => ((Element)element).ConstantSupported<Vertex>()))
                {
                    Vertex sum_ = new Vertex();
                    foreach (IWorkshopTree vert in ar.ParameterValues)
                        sum_ += (Vertex)((Element)vert).GetConstant();
                    return sum_.ToVector();
                }
                else
                {
                    Element sum_ = (Element)ar.ParameterValues[0];
                    for (int i = 1; i < ar.ParameterValues.Length; i++)
                        sum_ += (Element)ar.ParameterValues[i];
                    return sum_;
                }
            }
            else if (parameterValues[0] is V_EmptyArray)
            {
                return new V_Number(0);
            }
            else
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

    [CustomMethod("AverageOfArray", "Gets the average of an array.", CustomMethodType.MultiAction_Value)]
    class AverageOfArray : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[]
        {
            new CodeParameter("Array", "The array to average.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            if (parameterValues[0] is V_Array ar)
            {
                if (ar.ParameterValues.All(element => element is V_Number))
                    return new V_Number(ar.ParameterValues.Average(element => ((V_Number)element).Value));
                else if (ar.ParameterValues.All(element => ((Element)element).ConstantSupported<Vertex>()))
                {
                    Vertex sum_ = new Vertex();
                    foreach (IWorkshopTree vert in ar.ParameterValues)
                        sum_ += (Vertex)((Element)vert).GetConstant();
                    return (sum_ / ar.ParameterValues.Length).ToVector();
                }
                else
                {
                    Element sum_ = (Element)ar.ParameterValues[0];
                    for (int i = 1; i < ar.ParameterValues.Length; i++)
                        sum_ += (Element)ar.ParameterValues[i];
                    return sum_ / ar.ParameterValues.Length;
                }
            }
            else if (parameterValues[0] is V_EmptyArray)
            {
                return new V_Number(0);
            }
            else
            {
                IndexReference array = actionSet.VarCollection.Assign("_arrayToAverage", actionSet.IsGlobal, true);
                IndexReference sum = actionSet.VarCollection.Assign("_sumOfAverageArray", actionSet.IsGlobal, true);

                actionSet.AddAction(array.SetVariable((Element)parameterValues[0]));
                actionSet.AddAction(sum.SetVariable(0));
                ForeachBuilder builder = new ForeachBuilder(actionSet, array.GetVariable());
                actionSet.AddAction(sum.ModifyVariable(Operation.Add, builder.IndexValue));
                builder.Finish();
                return (Element)sum.GetVariable() / Element.Part<V_CountOf>(array.GetVariable());
            }
        }
    }
}
