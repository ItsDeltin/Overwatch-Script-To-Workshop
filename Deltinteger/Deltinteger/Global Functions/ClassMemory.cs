using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod ClassMemory(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ClassMemory",
            Documentation = "Gets the percentage of class memory taken.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => NumberOfClasses(actionSet) / 10
        };

        public static FuncMethod ClassMemoryUsed(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ClassMemoryUsed",
            Documentation = "Gets the number of classes that were created.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => NumberOfClasses(actionSet)
        };

        public static FuncMethod ClassMemoryRemaining(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ClassMemoryRemaining",
            Documentation = "Gets the remaining number of classes that can be created.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => Constants.MAX_ARRAY_LENGTH - NumberOfClasses(actionSet)
        };

        static Element NumberOfClasses(ActionSet actionSet)
        {
            return Element.CountOf(Element.Filter(
                // The number of assigned variables. Assigned variables do not equal 0.
                actionSet.Translate.DeltinScript.GetComponent<ClassData>().ClassIndexes.GetVariable(),
                Element.Compare(Element.ArrayElement(), Operator.NotEqual, Element.Num(0))
            ));
        } 
    }
}