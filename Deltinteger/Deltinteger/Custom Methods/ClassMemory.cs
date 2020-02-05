using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("ClassMemoryRemaining", "Gets the remaining number of classes that can be created.", CustomMethodType.Value)]
    public class ClassMemoryRemaining : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return Constants.MAX_ARRAY_LENGTH - Element.Part<V_CountOf>(actionSet.Translate.DeltinScript.SetupClasses().ClassIndexes.GetVariable());
        }

        public override CodeParameter[] Parameters() => null;
    }

    [CustomMethod("ClassMemoryUsed", "Gets the number of classes that were created.", CustomMethodType.Value)]
    public class ClassMemoryUsed : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return Element.Part<V_CountOf>(Element.Part<V_FilteredArray>(
                actionSet.Translate.DeltinScript.SetupClasses().ClassIndexes.GetVariable(),
                new V_Compare(new V_ArrayElement(), Operators.Equal, new V_Number(1))
            ));
        }

        public override CodeParameter[] Parameters() => null;
    }

    [CustomMethod("ClassMemory", "Gets the percentage of class memory taken.", CustomMethodType.Value)]
    public class ClassMemory : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return (Element.Part<V_CountOf>(actionSet.Translate.DeltinScript.SetupClasses().ClassIndexes.GetVariable()) / Constants.MAX_ARRAY_LENGTH) * 100;
        }

        public override CodeParameter[] Parameters() => null;
    }
}