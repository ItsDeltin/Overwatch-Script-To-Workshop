using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.CustomMethods;
using Deltin.Deltinteger.Parse.Lambda;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class ArrayType : CodeType
    {
        public CodeType ArrayOfType { get; }
        private readonly Scope _scope = new Scope();
        private readonly InternalVar _length;
        private readonly InternalVar _last;
        private readonly InternalVar _first;

        public ArrayType(CodeType arrayOfType) : base((arrayOfType?.Name ?? "define") + "[]")
        {
            ArrayOfType = arrayOfType;

            _length = new InternalVar("Length", CompletionItemKind.Property);
            _last = new InternalVar("Last", ArrayOfType, CompletionItemKind.Property);
            _first = new InternalVar("First", ArrayOfType, CompletionItemKind.Property);

            _scope.AddNativeVariable(_length);
            _scope.AddNativeVariable(_last);
            _scope.AddNativeVariable(_first);

            // Filtered Array
            Func(new FuncMethodBuilder() {
                Name = "FilteredArray",
                Documentation = "A copy of the specified array with any values that do not match the specified condition removed.",
                DoesReturnValue = true,
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("conditionLambda", "The condition that is evaluated for each element of the copied array. If the condition is true, the element is kept in the copied array.", new MacroLambda(null, ArrayOfType))
                },
                Action = (actionSet, methodCall) => GenericSort<V_FilteredArray>(actionSet, methodCall)
            });
            // Sorted Array
            Func(new FuncMethodBuilder() {
                Name = "SortedArray",
                Documentation = "A copy of the specified array with the values sorted according to the value rank that is evaluated for each element.",
                DoesReturnValue = true,
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("conditionLambda", "The value that is evaluated for each element of the copied array. The array is sorted by this rank in ascending order.", new MacroLambda(null, ArrayOfType))
                },
                Action = (actionSet, methodCall) => GenericSort<V_SortedArray>(actionSet, methodCall)
            });
            // Is True For Any
            Func(new FuncMethodBuilder() {
                Name = "IsTrueForAny",
                Documentation = "Whether the specified condition evaluates to true for any value in the specified array.",
                DoesReturnValue = true,
                Parameters = new CodeParameter[] {
                    new CodeParameter("conditionLambda", "The condition that is evaluated for each element of the specified array.", new MacroLambda(null, ArrayOfType))
                },
                Action = (actionSet, methodCall) => GenericSort<V_IsTrueForAny>(actionSet, methodCall)
            });
            // Is True For All
            Func(new FuncMethodBuilder() {
                Name = "IsTrueForAll",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                DoesReturnValue = true,
                Parameters = new CodeParameter[] {
                    new CodeParameter("conditionLambda", "The condition that is evaluated for each element of the specified array.", new MacroLambda(null, ArrayOfType))
                },
                Action = (actionSet, methodCall) => GenericSort<V_IsTrueForAll>(actionSet, methodCall)
            });
            // Contains
            Func(new FuncMethodBuilder() {
                Name = "Contains",
                Documentation = "Wether the array contains the specified value.",
                DoesReturnValue = true,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is being looked for in the array.", ArrayOfType)
                },
                Action = (actionSet, methodCall) => Element.Part<V_ArrayContains>(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Random
            Func(new FuncMethodBuilder() {
                Name = "Random",
                Documentation = "Gets a random value from the array.",
                DoesReturnValue = true,
                ReturnType = ArrayOfType,
                Action = (actionSet, methodCall) => Element.Part<V_RandomValueInArray>(actionSet.CurrentObject)
            });
            // Randomize
            Func(new FuncMethodBuilder() {
                Name = "Randomize",
                Documentation = "Returns a copy of the array that is randomized.",
                DoesReturnValue = true,
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.Part<V_RandomizedArray>(actionSet.CurrentObject)
            });
            // Append
            Func(new FuncMethodBuilder() {
                Name = "Append",
                Documentation = "A copy of the array with the specified value appended to it.",
                DoesReturnValue = true,
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is appended to the array. If the value is an array, it will be flattened.")
                },
                Action = (actionSet, methodCall) => Element.Part<V_Append>(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Remove
            Func(new FuncMethodBuilder() {
                Name = "Remove",
                Documentation = "A copy of the array with the specified value removed from it.",
                DoesReturnValue = true,
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is removed from the array.")
                },
                Action = (actionSet, methodCall) => Element.Part<V_RemoveFromArray>(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Slice
            Func(new FuncMethodBuilder() {
                Name = "Slice",
                Documentation = "A copy of the array containing only values from a specified index range.",
                DoesReturnValue = true,
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("startIndex", "The first index of the range."),
                    new CodeParameter("count", "The number of elements in the resulting array. The resulting array will contain fewer elements if the specified range exceeds the bounds of the array.")
                },
                Action = (actionSet, methodCall) => Element.Part<V_ArraySlice>(actionSet.CurrentObject, methodCall.ParameterValues[0], methodCall.ParameterValues[1])
            });
            // Index Of
            Func(new FuncMethodBuilder() {
                Name = "IndexOf",
                Documentation = "The index of a value within an array or -1 if no such value can be found.",
                DoesReturnValue = true,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value for which to search.")
                },
                Action = (actionSet, methodCall) => Element.Part<V_IndexOfArrayValue>(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
        }

        private static IWorkshopTree GenericSort<T>(ActionSet actionSet, MethodCall methodCall) where T: Element, new() => Element.Part<T>(actionSet.CurrentObject, ((LambdaAction)methodCall.ParameterValues[0]).Invoke(actionSet, new V_ArrayElement()));

        private void Func(FuncMethodBuilder builder)
        {
            _scope.AddNativeMethod(new FuncMethod(builder));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(_length, Element.Part<V_CountOf>(reference));
            assigner.Add(_last, Element.Part<V_LastOf>(reference));
            assigner.Add(_first, Element.Part<V_FirstOf>(reference));
        }

        public override bool Implements(CodeType type) => type is ArrayType arrayType && (ArrayOfType == null || arrayType.ArrayOfType == null || arrayType.ArrayOfType.Implements(ArrayOfType));
        public override Scope GetObjectScope() => _scope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();
    }
}