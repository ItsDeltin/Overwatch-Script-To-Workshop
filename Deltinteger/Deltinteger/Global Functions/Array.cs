using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod InsertValueInArray(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "InsertValueInArray",
            Documentation = "Inserts a value into an array.",
            ReturnType = deltinScript.Types.AnyArray(), 
            Parameters = new[] {
                new CodeParameter("array", "The array to modify.", deltinScript.Types.AnyArray()),
                new CodeParameter("index", "Where to insert the value.", deltinScript.Types.Number()),
                new CodeParameter("value", "The value to insert.", deltinScript.Types.Any())
            },
            Action = (actionSet, methodCall) => {
                Element array = methodCall.Get(0), index = methodCall.Get(1), value = methodCall.Get(2);

                return Element.Append(
                    Element.Append(
                        Element.Slice(array, Element.Num(0), index),
                        value
                    ),
                    Element.Slice(array, index, Element.Num(Constants.MAX_ARRAY_LENGTH))
                );
            }
        };

        public static FuncMethod RemoveFromArrayAtIndex(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "RemoveFromArrayAtIndex",
            Documentation = "Removes a value from an array by its index.",
            ReturnType = deltinScript.Types.AnyArray(), 
            Parameters = new[] {
                new CodeParameter("array", "The array to modify.", deltinScript.Types.AnyArray()),
                new CodeParameter("index", "The index to remove.", deltinScript.Types.Number()),
            },
            Action = (actionSet, methodCall) => {
                Element array = methodCall.Get(0), index = methodCall.Get(1);

                return Element.Append(
                    Element.Slice(array, Element.Num(0), index),
                    Element.Slice(array, index + 1, Element.Num(Constants.MAX_ARRAY_LENGTH))
                );
            }
        };
    }
}