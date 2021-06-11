using System;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Parse.Workshop;
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
        private readonly ITypeSupplier _supplier;

        public ArrayType(ITypeSupplier supplier, CodeType arrayOfType) : base(arrayOfType.GetNameOrAny() + "[]")
        {
            ArrayOfType = arrayOfType;
            ArrayHandler = arrayOfType.ArrayHandler;
            Attributes = arrayOfType.Attributes;
            DebugVariableResolver = new Debugger.ArrayResolver(ArrayOfType?.DebugVariableResolver, ArrayOfType?.GetName(), ArrayOfType is ClassType);

            Generics = new[] { arrayOfType };

            _length = new InternalVar("Length", supplier.Number(), CompletionItemKind.Property) { Ambiguous = false };
            _last = new InternalVar("Last", ArrayOfType, CompletionItemKind.Property) { Ambiguous = false };
            _first = new InternalVar("First", ArrayOfType, CompletionItemKind.Property) { Ambiguous = false };
            _supplier = supplier;

            Scope.AddNativeVariable(_length);
            Scope.AddNativeVariable(_last);
            Scope.AddNativeVariable(_first);

            var pipeType = new PipeType(ArrayOfType, this);
            var functionHandler = ArrayOfType.ArrayHandler.GetFunctionHandler();

            // Filtered Array
            new GenericSortFunction()
            {
                Name = "FilteredArray",
                Documentation = "A copy of the specified array with any values that do not match the specified condition removed.",
                ReturnType = this,
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The condition that is evaluated for each element of the copied array. If the condition is true, the element is kept in the copied array.",
                Function = "Filtered Array",
                Executor = functionHandler.FilteredArray()
            }.Add(Scope, supplier);
            // Sorted Array
            new GenericSortFunction()
            {
                Name = "SortedArray",
                Documentation = "A copy of the specified array with the values sorted according to the value rank that is evaluated for each element.",
                ReturnType = this,
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The value that is evaluated for each element of the copied array. The array is sorted by this rank in ascending order.",
                Function = "Sorted Array",
                Executor = functionHandler.SortedArray()
            }.Add(Scope, supplier);
            // Is True For Any
            new GenericSortFunction()
            {
                Name = "IsTrueForAny",
                Documentation = "Whether the specified condition evaluates to true for any value in the specified array.",
                ReturnType = supplier.Boolean(),
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array.",
                Function = "Is True For Any",
                Executor = functionHandler.Any()
            }.Add(Scope, supplier);
            // Is True For All
            new GenericSortFunction()
            {
                Name = "IsTrueForAll",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                ReturnType = supplier.Boolean(),
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Boolean(),
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array.",
                Function = "Is True For All",
                Executor = functionHandler.All()
            }.Add(Scope, supplier);
            // Mapped
            if (functionHandler.AllowUnhandled)
            new GenericSortFunction()
            {
                Name = "Map",
                Documentation = "Whether the specified condition evaluates to true for every value in the specified array.",
                ReturnType = supplier.Any(),
                ArrayOfType = ArrayOfType,
                FuncType = supplier.Any(),
                ParameterDocumentation = "The condition that is evaluated for each element of the specified array.",
                Function = "Mapped Array",
                Executor = functionHandler.Map()
            }.Add(Scope, supplier);
            // Contains
            Func(new FuncMethodBuilder()
            {
                Name = "Contains",
                Documentation = "Whether the array contains the specified value.",
                ReturnType = supplier.Boolean(),
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is being looked for in the array.", ArrayOfType)
                },
                Action = (actionSet, methodCall) => functionHandler.Contains(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Random
            if (functionHandler.AllowUnhandled)
            Func(new FuncMethodBuilder()
            {
                Name = "Random",
                Documentation = "Gets a random value from the array.",
                ReturnType = ArrayOfType,
                Action = (actionSet, methodCall) => Element.Part("Random Value In Array", actionSet.CurrentObject)
            });
            // Randomize
            if (functionHandler.AllowUnhandled)
            Func(new FuncMethodBuilder()
            {
                Name = "Randomize",
                Documentation = "Returns a copy of the array that is randomized.",
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.Part("Randomized Array", actionSet.CurrentObject)
            });
            // Append
            if (functionHandler.AllowUnhandled)
            Func(new FuncMethodBuilder()
            {
                Name = "Append",
                Documentation = "A copy of the array with the specified value appended to it.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is appended to the array. If the value is an array, it will be flattened.", pipeType)
                },
                Action = (actionSet, methodCall) => Element.Append(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Remove
            if (functionHandler.AllowUnhandled)
            Func(new FuncMethodBuilder()
            {
                Name = "Remove",
                Documentation = "A copy of the array with the specified value removed from it.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is removed from the array.", pipeType)
                },
                Action = (actionSet, methodCall) => Element.Part("Remove From Array", actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Slice
            if (functionHandler.AllowUnhandled)
            Func(new FuncMethodBuilder()
            {
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
            if (functionHandler.AllowUnhandled)
            Func(new FuncMethodBuilder()
            {
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
            Operations.AddTypeOperation(new[] {
                // + append
                new TypeOperation(TypeOperator.Add, pipeType, this, (l, r) => Element.Append(l, r)),
                // - remove
                new TypeOperation(TypeOperator.Subtract, pipeType, this, (l, r) => Element.Remove(l, r))
            });
            Operations.AddTypeOperation(new[] {
                // += mod append
                new AssignmentOperation(AssignmentOperator.AddEqual, pipeType, info => info.Modify(Operation.AppendToArray)),
                // -= mod remove
                new AssignmentOperation(AssignmentOperator.SubtractEqual, pipeType, info => info.Modify(Operation.RemoveFromArrayByValue))
            });

            arrayOfType.ArrayHandler.OverrideArray(this);
        }

        private void Func(FuncMethodBuilder builder)
        {
            Scope.AddNativeMethod(new FuncMethod(builder));
        }

        public override IGettableAssigner GetGettableAssigner(AssigningAttributes attributes)
        {
            var overrideAssigner = ArrayOfType.ArrayHandler.GetArrayAssigner(attributes);
            if (overrideAssigner != null)
                return overrideAssigner;

            return new DataTypeAssigner(attributes);
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            var functionHandler = ArrayOfType.ArrayHandler.GetFunctionHandler();
            assigner.Add(_length, functionHandler.Length(reference));
            assigner.Add(_first, functionHandler.FirstOf(reference));
            assigner.Add(_last, functionHandler.LastOf(reference));
        }

        public override AnonymousType[] ExtractAnonymousTypes() => ArrayOfType.ExtractAnonymousTypes();

        // public override bool Implements(CodeType type) => (type is ArrayType arrayType && arrayType.ArrayOfType.Implements(ArrayOfType)) || (ArrayOfType is IAdditionalArray additon && additon.AlternateImplements(type));
        public override Scope GetObjectScope() => Scope;
        protected override bool DoesImplement(CodeType type) => type is AnyType || ArrayOfType is AnyType || (type is ArrayType arrayType && arrayType.ArrayOfType.Implements(ArrayOfType));
        public override bool Is(CodeType type) => type is ArrayType other && ArrayOfType.Is(other.ArrayOfType);
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();

        public override string GetName(bool makeAnonymousTypesUnknown = false)
        {
            string result = ArrayOfType.GetName(makeAnonymousTypesUnknown);
            if (ArrayOfType is PortableLambdaType) result = "(" + result + ")";
            return result + "[]";
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            // Do nothing if the ArrayOfType does not contain generics.
            if (!ArrayOfType.Attributes.ContainsGenerics)
                return this;
            
            // Otherwise, create a new ArrayType with the array type converted.
            return new ArrayType(_supplier, ArrayOfType.GetRealType(instanceInfo));
        }
    }

    class SourceVariableResolver
    {
        public IVariableInstance Calling { get; private set; }

        public static object GetSourceVariable(ParseInfo parseInfo, DocRange range)
        {
            var resolver = new SourceVariableResolver();
            parseInfo.SourceExpression.OnResolve(expr => {
                // Make sure the expression is a variable call.
                if (expr is CallVariableAction variableCall && variableCall.Calling.Provider.VariableType != VariableType.ElementReference)
                    resolver.Calling = variableCall.Calling;
                // Otherwise, add an error.
                else
                    parseInfo.Script.Diagnostics.Error("Functions that directly modify arrays requires a variable as the source.", range);
            });
            return resolver;
        }

        public static IGettable GetIndexReference(ActionSet actionSet, MethodCall methodCall) => actionSet.IndexAssigner[((SourceVariableResolver)methodCall.AdditionalData).Calling.Provider];

        public static IWorkshopTree Modify(ActionSet actionSet, MethodCall methodCall, Operation operation)
        {
            // var calling = SourceVariableResolver.GetIndexReference(actionSet, methodCall);
            // calling.Modify(actionSet, operation, value: methodCall.ParameterValues[0], target: actionSet.CurrentObjectRelatedIndex.Target);
            // return Element.CountOf(calling.GetVariable());

            actionSet.CurrentObjectRelatedIndex.Reference.Modify(actionSet, operation, value: methodCall.ParameterValues[0], target: actionSet.CurrentObjectRelatedIndex.Target);
            return (Element)0;
        }
    }
}