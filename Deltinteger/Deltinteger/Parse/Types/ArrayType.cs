using System;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Lambda;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class ArrayType : CodeType
    {
        public CodeType ArrayOfType { get; }
        public Scope Scope { get; } = new Scope();
        private readonly InternalVar _length;
        private readonly InternalVar _last;
        private readonly InternalVar _first;

        public ArrayType(ITypeSupplier supplier, CodeType arrayOfType) : base(arrayOfType.GetName() + "[]")
        {
            ArrayOfType = arrayOfType;
            DebugVariableResolver = new Debugger.ArrayResolver(ArrayOfType?.DebugVariableResolver, ArrayOfType?.GetName(), ArrayOfType is ClassType);

            _length = new InternalVar("Length", CompletionItemKind.Property) { Ambiguous = false };
            _last = new InternalVar("Last", ArrayOfType, CompletionItemKind.Property) { Ambiguous = false };
            _first = new InternalVar("First", ArrayOfType, CompletionItemKind.Property) { Ambiguous = false };

            Scope.AddNativeVariable(_length);
            Scope.AddNativeVariable(_last);
            Scope.AddNativeVariable(_first);

            var pipeType = new PipeType(ArrayOfType, this);

            // Filtered Array
            new GenericSortFunction() {
                Name = "FilteredArray",
                Documentation = "A copy of the specified array with any values that do not match the specified condition removed.",
                ReturnType = this,
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The condition that is evaluated for each element of the copied array. If the condition is true, the element is kept in the copied array."
            }.Add("Filtered Array", Scope, supplier);
            // Sorted Array
            new GenericSortFunction() {
                Name = "SortedArray",
                Documentation = "A copy of the specified array with the values sorted according to the value rank that is evaluated for each element.",
                ReturnType = this,
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The value that is evaluated for each element of the copied array. The array is sorted by this rank in ascending order."
            }.Add("Sorted Array", Scope, supplier);
            // Is True For Any
            new GenericSortFunction() {
                Name = "IsTrueForAny",
                Documentation = "Whether the specified condition evaluates to true for any value in the specified array.",
                ReturnType = supplier.Boolean(),
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array."
            }.Add("Is True For Any", Scope, supplier);
            // Is True For All
            new GenericSortFunction() {
                Name = "IsTrueForAll",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                ReturnType = supplier.Boolean(),
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array."
            }.Add("Is True For All", Scope, supplier);
            // Mapped
            new GenericSortFunction() {
                Name = "Map",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                ReturnType = supplier.Any(),
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Any(),
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array."
            }.Add("Mapped Array", Scope, supplier);
            // Contains
            Func(new FuncMethodBuilder() {
                Name = "Contains",
                Documentation = "Wether the array contains the specified value.",
                ReturnType = supplier.Boolean(),
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is being looked for in the array.", ArrayOfType)
                },
                Action = (actionSet, methodCall) => Element.Contains(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Random
            Func(new FuncMethodBuilder() {
                Name = "Random",
                Documentation = "Gets a random value from the array.",
                ReturnType = ArrayOfType,
                Action = (actionSet, methodCall) => Element.Part("Random Value In Array", actionSet.CurrentObject)
            });
            // Randomize
            Func(new FuncMethodBuilder() {
                Name = "Randomize",
                Documentation = "Returns a copy of the array that is randomized.",
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.Part("Randomized Array", actionSet.CurrentObject)
            });
            // Append
            Func(new FuncMethodBuilder() {
                Name = "Append",
                Documentation = "A copy of the array with the specified value appended to it.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is appended to the array. If the value is an array, it will be flattened.", pipeType)
                },
                Action = (actionSet, methodCall) => Element.Append(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Remove
            Func(new FuncMethodBuilder() {
                Name = "Remove",
                Documentation = "A copy of the array with the specified value removed from it.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is removed from the array.", pipeType)
                },
                Action = (actionSet, methodCall) => Element.Part("Remove From Array", actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Slice
            Func(new FuncMethodBuilder() {
                Name = "Slice",
                Documentation = "A copy of the array containing only values from a specified index range.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("startIndex", "The first index of the range.", supplier.Number()),
                    new CodeParameter("count", "The number of elements in the resulting array. The resulting array will contain fewer elements if the specified range exceeds the bounds of the array.", supplier.Number())
                },
                Action = (actionSet, methodCall) => Element.Part("Array Slice", actionSet.CurrentObject, methodCall.ParameterValues[0], methodCall.ParameterValues[1])
            });
            // Index Of
            Func(new FuncMethodBuilder() {
                Name = "IndexOf",
                Documentation = "The index of a value within an array or -1 if no such value can be found.",
                ReturnType = supplier.Number(),
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value for which to search.", arrayOfType)
                },
                Action = (actionSet, methodCall) => Element.IndexOfArrayValue(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Modify Append
            Func(new FuncMethodBuilder() {
                Name = "ModAppend",
                Documentation = "Appends a value to the array. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is pushed to the array.", pipeType)
                },
                OnCall = SourceVariableResolver.GetSourceVariable,
                ReturnType = supplier.Number(),
                Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, methodCall, Operation.AppendToArray)
            });
            // Modify Remove By Value
            Func(new FuncMethodBuilder() {
                Name = "ModRemoveByValue",
                Documentation = "Removes an element from the array by a value. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is removed from the array.", arrayOfType)
                },
                OnCall = SourceVariableResolver.GetSourceVariable,
                ReturnType = supplier.Number(),
                Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, methodCall, Operation.RemoveFromArrayByValue)
            });
            // Modify Remove By Index
            Func(new FuncMethodBuilder() {
                Name = "ModRemoveByIndex",
                Documentation = "Removes an element from the array by the index. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("index", "The index of the element that is removed from the array.", supplier.Number())
                },
                OnCall = SourceVariableResolver.GetSourceVariable,
                ReturnType = supplier.Number(),
                Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, methodCall, Operation.RemoveFromArrayByIndex)
            });

            // Add type operations.
            Operations = new ITypeOperation[] {
                new TypeOperation(TypeOperator.Add, pipeType, this, (l, r) => Element.Append(l, r)),
                new TypeOperation(TypeOperator.Subtract, pipeType, this, (l, r) => Element.Remove(l, r))
            };

            if (arrayOfType is IAdditionalArray addition)
                addition.OverrideArray(this);
        }

        private void Func(FuncMethodBuilder builder)
        {
            Scope.AddNativeMethod(new FuncMethod(builder));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(_length, Element.CountOf(reference));
            assigner.Add(_last, Element.LastOf(reference));
            assigner.Add(_first, Element.FirstOf(reference));
        }

        // public override bool Implements(CodeType type) => (type is ArrayType arrayType && arrayType.ArrayOfType.Implements(ArrayOfType)) || (ArrayOfType is IAdditionalArray additon && additon.AlternateImplements(type));
        public override Scope GetObjectScope() => Scope;
        public override bool Implements(CodeType type) => type is ArrayType arrayType && arrayType.ArrayOfType.Implements(ArrayOfType);
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();

        public override string GetName()
        {
            string result = ArrayOfType.GetName();
            if (ArrayOfType is PortableLambdaType) result = "(" + result + ")";
            return result + "[]"; 
        }
    }

    class GenericSortFunction
    {
        public string Name;
        public string Documentation;
        public string ParameterDocumentation;
        public CodeType ReturnType;
        public CodeType ArrayOfType;
        public CodeType FuncType;

        public void Add(string function, Scope addToScope, ITypeSupplier supplier)
        {
            // value => ...
            var noIndex = GetFuncMethod();
            noIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", ParameterDocumentation, new MacroLambda(FuncType, ArrayOfType))
            };
            noIndex.Action = (actionSet, methodCall) =>
                Element.Part(function, actionSet.CurrentObject, ((ILambdaInvocable)methodCall.ParameterValues[0]).Invoke(actionSet, Element.ArrayElement()));

            // (value, index) => ...
            var withIndex = GetFuncMethod();
            withIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", ParameterDocumentation, new MacroLambda(FuncType, ArrayOfType, supplier.Number()))
            };
            withIndex.Action = (actionSet, methodCall) =>
                Element.Part(function, actionSet.CurrentObject, ((ILambdaInvocable)methodCall.ParameterValues[0]).Invoke(actionSet, Element.ArrayElement(), Element.ArrayIndex()));
            
            addToScope.AddNativeMethod(new FuncMethod(noIndex));
            addToScope.AddNativeMethod(new FuncMethod(withIndex));
        }

        private FuncMethodBuilder GetFuncMethod() => new FuncMethodBuilder() {
            Name = Name,
            Documentation = Documentation,
            ReturnType = ReturnType
        };
    }

    interface IAdditionalArray
    {
        void OverrideArray(ArrayType array);
    }

    class SourceVariableResolver
    {
        public IIndexReferencer Calling { get; private set; }

        public static object GetSourceVariable(ParseInfo parseInfo, DocRange range)
        {
            var resolver = new SourceVariableResolver();
            parseInfo.SourceExpression.OnResolve(expr => {
                // Make sure the expression is a variable call.
                if (expr is CallVariableAction variableCall && variableCall.Calling.VariableType != VariableType.ElementReference)
                    resolver.Calling = variableCall.Calling;
                // Otherwise, add an error.
                else
                    parseInfo.Script.Diagnostics.Error("Functions that directly modify arrays requires a variable as the source.", range);
            });
            return resolver;
        }

        public static IndexReference GetIndexReference(ActionSet actionSet, MethodCall methodCall) => (IndexReference)actionSet.IndexAssigner[((SourceVariableResolver)methodCall.AdditionalData).Calling];

        public static IWorkshopTree Modify(ActionSet actionSet, MethodCall methodCall, Operation operation)
        {
            var calling = SourceVariableResolver.GetIndexReference(actionSet, methodCall);
            actionSet.AddAction(calling.ModifyVariable(operation, methodCall.Get(0), actionSet.CurrentObjectRelatedIndex.Target));
            return Element.CountOf(calling.Get());
        }
    }
}