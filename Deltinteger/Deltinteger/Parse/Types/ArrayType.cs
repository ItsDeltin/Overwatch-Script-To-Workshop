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
            new GenericSortFunction() {
                Name = "FilteredArray",
                Documentation = "A copy of the specified array with any values that do not match the specified condition removed.",
                ReturnType = this,
                ArrayOfType = ArrayOfType,
                ParameterDocumentation = "The condition that is evaluated for each element of the copied array. If the condition is true, the element is kept in the copied array."
            }.Add<V_FilteredArray>(_scope);
            // Sorted Array
            new GenericSortFunction() {
                Name = "SortedArray",
                Documentation = "A copy of the specified array with the values sorted according to the value rank that is evaluated for each element.",
                ReturnType = this,
                ArrayOfType = ArrayOfType,
                ParameterDocumentation = "The value that is evaluated for each element of the copied array. The array is sorted by this rank in ascending order."
            }.Add<V_SortedArray>(_scope);
            // Is True For Any
            new GenericSortFunction() {
                Name = "IsTrueForAny",
                Documentation = "Whether the specified condition evaluates to true for any value in the specified array.",
                ArrayOfType = ArrayOfType,
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array."
            }.Add<V_IsTrueForAny>(_scope);
            // Is True For All
            new GenericSortFunction() {
                Name = "IsTrueForAll",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                ArrayOfType = ArrayOfType,
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array."
            }.Add<V_IsTrueForAll>(_scope);
            // Mapped
            new GenericSortFunction() {
                Name = "Map",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                ArrayOfType = ArrayOfType,
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array."
            }.Add<V_MappedArray>(_scope);
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

    class GenericSortFunction
    {
        public string Name;
        public string Documentation;
        public string ParameterDocumentation;
        public CodeType ReturnType;
        public CodeType ArrayOfType;

        public void Add<T>(Scope addToScope) where T: Element, new()
        {
            // value => ...
            var noIndex = GetFuncMethod();
            noIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", ParameterDocumentation, new MacroLambda(null, ArrayOfType))
            };
            noIndex.Action = (actionSet, methodCall) =>
                Element.Part<T>(actionSet.CurrentObject, ((LambdaAction)methodCall.ParameterValues[0]).Invoke(actionSet, new V_ArrayElement()));

            // (value, index) => ...
            var withIndex = GetFuncMethod();
            withIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", ParameterDocumentation, new MacroLambda(null, ArrayOfType, null))
            };
            withIndex.Action = (actionSet, methodCall) =>
                Element.Part<T>(actionSet.CurrentObject, ((LambdaAction)methodCall.ParameterValues[0]).Invoke(actionSet, new V_ArrayElement(), new V_CurrentArrayIndex()));
            
            addToScope.AddNativeMethod(new FuncMethod(noIndex));
            addToScope.AddNativeMethod(new FuncMethod(withIndex));
        }

        private FuncMethodBuilder GetFuncMethod() => new FuncMethodBuilder() {
            Name = Name,
            Documentation = Documentation,
            ReturnType = ReturnType,
            DoesReturnValue = true
        };
    }
}