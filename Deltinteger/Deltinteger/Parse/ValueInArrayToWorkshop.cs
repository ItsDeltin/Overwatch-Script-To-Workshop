using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    static class ValueInArrayToWorkshop
    {
        public static IWorkshopTree ValueInArray(IWorkshopTree array, IWorkshopTree index)
        {
            if (array is IStructValue structArray)
                return new ValueInStructArray(structArray, index);
            
            return Element.ValueInArray(array, index);
        }

        public static IStructValue ExtractStructValue(IWorkshopTree value)
        {
            // Struct value.
            if (value is IStructValue structValue) return structValue;

            // Empty array.
            if (value is Element element &&
                (element.Function.Name == "Empty Array" ||
                (element.Function.Name == "Array" && element.ParameterValues.Length == 0)))
                return new StructArray(new IStructValue[0]);
            
            // Unknown
            throw new Exception(value.ToString() + " is not a valid struct value.");
        }
    }
}