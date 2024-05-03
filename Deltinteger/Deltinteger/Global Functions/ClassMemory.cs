using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    using static Element;

    partial class GlobalFunctions
    {
        public static FuncMethod ClassMemory(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "ClassMemory",
            Documentation = "Gets the percentage of class memory taken.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => NumberOfClasses(actionSet) / 10
        };

        public static FuncMethod ClassMemoryUsed(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "ClassMemoryUsed",
            Documentation = "Gets the number of classes that were created.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => NumberOfClasses(actionSet)
        };

        public static FuncMethod ClassMemoryRemaining(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "ClassMemoryRemaining",
            Documentation = "Gets the remaining number of classes that can be created.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => Constants.MAX_ARRAY_LENGTH - NumberOfClasses(actionSet)
        };

        public static FuncMethod TruncateClassData(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "TruncateClassData",
            Documentation = "Cleans up the workshop variables used to manage and store classes.",
            Action = (actionSet, methodCall) =>
            {
                // Find the last unassigned value
                var classData = actionSet.ToWorkshop.GetComponent<ClassData>();
                var indexes = classData.ClassIndexes.GetVariable();

                var truncateStart = actionSet.VarCollection.Assign("truncateClass", actionSet.IsGlobal, false);
                truncateStart.Set(actionSet, CountOf(indexes) - IndexOfArrayValue(
                    Map(
                        // Reverse the array
                        Sort(indexes, -ArrayIndex()),
                        // All elements are either True for unallocated, or False for allocated.
                        Not(ArrayElement())
                    ),
                    // Find the first allocated element
                    False()
                ));

                // Truncate indexer and variable stacks
                foreach (var truncate in new[] { classData.ClassIndexes }.Concat(deltinScript.WorkshopConverter.ClassInitializer.Stacks))
                    truncate.Set(actionSet, Slice(truncate.Get(), Num(0), truncateStart.Get()));

                return null;
            }
        };

        public static FuncMethod DeleteAllClasses(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "DeleteAllClasses",
            Documentation = "Deletes all allocated classes. " +
            "Any variables containing a class reference will point to invalid data after the classes are deleted.",
            Action = (actionSet, methodCall) =>
            {
                // Set classIndexes to [-1].
                actionSet.ToWorkshop.GetComponent<ClassData>().ClassIndexes.Set(actionSet, CreateArray(Num(-1)));

                // Clear object variables.
                foreach (var objectVariable in deltinScript.WorkshopConverter.ClassInitializer.Stacks)
                    objectVariable.Set(actionSet, EmptyArray());

                return null;
            }
        };

        public static FuncMethod IsClassReferenceValid(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "IsClassReferenceValid",
            Documentation = "Returns true if the input value is not null. If class_generations is enabled, this will additionally check if the class reference is valid.",
            ReturnType = deltinScript.Types.Boolean(),
            Parameters = [
                new CodeParameter("classReference", deltinScript.Types.Any())
            ],
            Action = (actionSet, methodCall) =>
            {
                if (actionSet.DeltinScript.Settings.TrackClassGenerations)
                    return actionSet.DeltinScript.GetComponent<ClassData>().IsReferenceValid(methodCall.Get(0));
                return methodCall.Get(0);
            }
        };

        static Element NumberOfClasses(ActionSet actionSet)
        {
            return Element.CountOf(Element.Filter(
                // The number of assigned variables. Assigned variables do not equal 0.
                actionSet.Translate.DeltinScript.GetComponent<ClassData>().ClassIndexes.GetVariable(),
                ArrayElement()
            )) - 1;
        }
    }
}