using System;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    static class StructHelper
    {
        public static IWorkshopTree ExtractArbritraryValue(IWorkshopTree workshopValue)
        {
            IWorkshopTree current = workshopValue;
            while (current is IStructValue step)
                current = step.GetArbritraryValue();

            return current;
        }

        public static IWorkshopTree ValueInArray(IWorkshopTree array, IWorkshopTree index)
        {
            if (array is IStructValue structArray)
                return new ValueInStructArray(structArray, index);

            return Element.ValueInArray(array, index);
        }

        public static IWorkshopTree CreateArray(IWorkshopTree[] elements)
        {
            // Struct array
            if (elements.Any(value => value is IStructValue))
            {
                // Ensure that all the values are structs.
                if (!elements.All(value => value is IStructValue))
                    throw new Exception("Cannot mix normal and struct values in an array");

                return new StructArray(Array.ConvertAll(elements, item => (IStructValue)item));
            }

            // Normal array
            return Element.CreateArray(elements);
        }

        public static IWorkshopTree BridgeIfRequired(IWorkshopTree value, Func<IWorkshopTree, IWorkshopTree> converter)
        {
            if (value is IStructValue structValue)
                return structValue.Bridge(args => converter(args.Value));

            return converter(value);
        }

        public static IStructValue ExtractStructValue(IWorkshopTree value)
        {
            // Struct value.
            if (value is IStructValue structValue) return structValue;

            // Empty array.
            var emptyArray = MakeEmptyArray(value);
            if (emptyArray != null) return emptyArray;

            // Unknown
            throw new Exception(value.ToString() + " is not a valid struct value.");
        }

        static StructArray MakeEmptyArray(IWorkshopTree value)
        {
            if (value is Element element)
            {
                if (element.Function.Name == "Empty Array" || element.Function.Name == "Null")
                    return new StructArray(new IStructValue[0]);

                else if (element.Function.Name == "Array")
                {
                    var arr = new IStructValue[element.ParameterValues.Length];
                    for (int i = 0; i < element.ParameterValues.Length; i++)
                    {
                        arr[i] = MakeEmptyArray(element.ParameterValues[i]);
                        if (arr[i] == null) return null;
                    }

                    return new StructArray(arr);
                }
            }
            return null;
        }
    }
}