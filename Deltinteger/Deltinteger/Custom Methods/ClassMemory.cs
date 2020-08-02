using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("ClassMemoryRemaining", "Gets the remaining number of classes that can be created.", CustomMethodType.Value)]
    public class ClassMemoryRemaining : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return Constants.MAX_ARRAY_LENGTH - ClassMemoryUsed.NumberOfClasses(actionSet);
        }

        public override CodeParameter[] Parameters() => null;
    }

    [CustomMethod("ClassMemoryUsed", "Gets the number of classes that were created.", CustomMethodType.Value)]
    public class ClassMemoryUsed : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return NumberOfClasses(actionSet);
        }

        public override CodeParameter[] Parameters() => null;

        public static Element NumberOfClasses(ActionSet actionSet)
        {
            return Element.Part<V_CountOf>(Element.Part<V_FilteredArray>(
                // The number of assigned variables. Assigned variables do not equal 0.
                actionSet.Translate.DeltinScript.GetComponent<ClassData>().ClassIndexes.GetVariable(),
                new V_Compare(new V_ArrayElement(), Operator.NotEqual, new V_Number(0))
            ));
        } 
    }

    [CustomMethod("ClassMemory", "Gets the percentage of class memory taken.", CustomMethodType.Value)]
    public class ClassMemory : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            //return (ClassMemoryUsed.NumberOfClasses(actionSet) / Constants.MAX_ARRAY_LENGTH) * 100;
            return ClassMemoryUsed.NumberOfClasses(actionSet) / 10;
        }

        public override CodeParameter[] Parameters() => null;
    }
}