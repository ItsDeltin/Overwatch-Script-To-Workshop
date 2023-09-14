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
        public Scope Scope
        {
            get
            {
                SetupScope();
                return _scopeInstance;
            }
        }

        public override TypeOperatorInfo Operations
        {
            get
            {
                SetupScope();
                return _operationsInstance;
            }
        }

        readonly IVariableInstance _length;
        readonly IVariableInstance _last;
        readonly IVariableInstance _first;
        readonly ITypeSupplier _supplier;
        Scope _scopeInstance;
        TypeOperatorInfo _operationsInstance;

        public ArrayType(ITypeSupplier supplier, CodeType arrayOfType) : base(arrayOfType.GetNameOrAny() + "[]")
        {
            ArrayOfType = arrayOfType;
            ArrayHandler = arrayOfType.ArrayHandler;
            Attributes = arrayOfType.Attributes;
            TypeSemantics = arrayOfType.TypeSemantics;
            AsReferenceResetSettability = arrayOfType.AsReferenceResetSettability;
            DebugVariableResolver = new Debugger.ArrayResolver(ArrayOfType?.DebugVariableResolver, ArrayOfType?.GetName(), ArrayOfType is ClassType);

            Generics = new[] { arrayOfType };

            (_length, _first, _last) = supplier.ArrayProvider().GetInstances(this, arrayOfType);
            _supplier = supplier;
        }

        void SetupScope()
        {
            if (_scopeInstance != null) return;

            _scopeInstance = new Scope();
            _operationsInstance = new TypeOperatorInfo(this);
            _operationsInstance.DefaultAssignment = ArrayOfType.Operations.DefaultAssignment;

            Scope.AddNativeVariable(_length);
            Scope.AddNativeVariable(_last);
            Scope.AddNativeVariable(_first);

            var pipeType = new PipeType(ArrayOfType, this);
            var allowUnhandled = ArrayOfType.ArrayHandler.GetFunctionHandler().AllowUnhandled;

            // Filtered Array
            MakeGenericSortFunction(
                name: "FilteredArray",
                documentation: "A copy of the specified array with any values that do not match the specified condition removed.",
                returnType: this,
                funcType: _supplier.Boolean(),
                parameterDocumentation: "The condition that is evaluated for each element of the copied array. If the condition is true, the element is kept in the copied array.",
                pointToExecutor: h => h.FilteredArray()
            ).Add(Scope, _supplier);
            // Sorted Array
            MakeGenericSortFunction(
                name: "SortedArray",
                documentation: "A copy of the specified array with the values sorted according to the value rank that is evaluated for each element.",
                returnType: this,
                funcType: _supplier.Number(),
                parameterDocumentation: "The value that is evaluated for each element of the copied array. The array is sorted by this rank in ascending order.",
                pointToExecutor: h => h.SortedArray()
            ).Add(Scope, _supplier);
            // Is True For Any
            MakeGenericSortFunction(
                name: "IsTrueForAny",
                documentation: "Whether the specified condition evaluates to true for any value in the specified array.",
                returnType: _supplier.Boolean(),
                funcType: _supplier.Boolean(),
                parameterDocumentation: "The condition that is evaluated for each element of the specified array.",
                pointToExecutor: h => h.Any()
            ).Add(Scope, _supplier);
            // Is True For All
            MakeGenericSortFunction(
                name: "IsTrueForAll",
                documentation: "Whether the specified condition evaluates to true for every value in the specified array.",
                returnType: _supplier.Boolean(),
                funcType: _supplier.Boolean(),
                parameterDocumentation: "The condition that is evaluated for each element of the specified array.",
                pointToExecutor: h => h.All()
            ).Add(Scope, _supplier);
            // Mapped
            var mapGenericParameter = new AnonymousType("U", new AnonymousTypeAttributes(false));
            var mapmethodInfo = new MethodInfo(new[] { mapGenericParameter });
            mapGenericParameter.Context = mapmethodInfo.Tracker;
            MakeGenericSortFunction(
                name: "Map",
                documentation: "Whether the specified condition evaluates to true for every value in the specified array.",
                returnType: new ArrayType(_supplier, mapGenericParameter),
                funcType: mapGenericParameter,
                parameterDocumentation: "The condition that is evaluated for each element of the specified array.",
                pointToExecutor: h => h.Map(),
                methodInfo: mapmethodInfo
            ).Add(Scope, _supplier);
            // Contains
            Func(new FuncMethodBuilder()
            {
                Name = "Contains",
                Documentation = "Whether the array contains the specified value.",
                ReturnType = _supplier.Boolean(),
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is being looked for in the array.", ArrayOfType)
                },
                Action = (actionSet, methodCall) => GetFunctionHandler(actionSet).Contains(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Random
            if (allowUnhandled)
                Func(new FuncMethodBuilder()
                {
                    Name = "Random",
                    Documentation = "Gets a random value from the array.",
                    ReturnType = ArrayOfType,
                    Action = (actionSet, methodCall) => Element.Part("Random Value In Array", actionSet.CurrentObject)
                });
            // Randomize
            if (allowUnhandled)
                Func(new FuncMethodBuilder()
                {
                    Name = "Randomize",
                    Documentation = "Returns a copy of the array that is randomized.",
                    ReturnType = this,
                    Action = (actionSet, methodCall) => Element.Part("Randomized Array", actionSet.CurrentObject)
                });
            // Append
            Func(new FuncMethodBuilder()
            {
                Name = "Append",
                Documentation = "A copy of the array with the specified value appended to it.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is appended to the array. If the value is an array, it will be flattened.", pipeType)
                },
                Action = (actionSet, methodCall) => GetFunctionHandler(actionSet).Append(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Remove
            if (allowUnhandled)
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
            Func(new FuncMethodBuilder()
            {
                Name = "Slice",
                Documentation = "A copy of the array containing only values from a specified index range.",
                ReturnType = this,
                Parameters = new CodeParameter[] {
                    new CodeParameter("startIndex", "The first index of the range.", _supplier.Number()),
                    new CodeParameter("count", "The number of elements in the resulting array. The resulting array will contain fewer elements if the specified range exceeds the bounds of the array.", _supplier.Number())
                },
                Action = (actionSet, methodCall) => GetFunctionHandler(actionSet).Slice(actionSet.CurrentObject, methodCall.ParameterValues[0], methodCall.ParameterValues[1])
            });
            // Index Of
            Func(new FuncMethodBuilder()
            {
                Name = "IndexOf",
                Documentation = "The index of a value within an array or -1 if no such value can be found.",
                ReturnType = _supplier.Number(),
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value for which to search.", ArrayOfType)
                },
                Action = (actionSet, methodCall) => GetFunctionHandler(actionSet).IndexOf(actionSet.CurrentObject, methodCall.ParameterValues[0])
            });
            // Modify Append
            Func(new FuncMethodBuilder()
            {
                Name = "ModAppend",
                Documentation = "Appends a value to the array. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is pushed to the array.", pipeType)
                },
                OnCall = SourceVariableResolver.GetSourceVariable,
                ReturnType = _supplier.Number(),
                Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, methodCall, Operation.AppendToArray)
            });
            // Modify Remove By Value
            Func(new FuncMethodBuilder()
            {
                Name = "ModRemoveByValue",
                Documentation = "Removes an element from the array by a value. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("value", "The value that is removed from the array.", ArrayOfType)
                },
                OnCall = SourceVariableResolver.GetSourceVariable,
                ReturnType = _supplier.Number(),
                Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, methodCall, Operation.RemoveFromArrayByValue)
            });
            // Modify Remove By Index
            Func(new FuncMethodBuilder()
            {
                Name = "ModRemoveByIndex",
                Documentation = "Removes an element from the array by the index. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("index", "The index of the element that is removed from the array.", _supplier.Number())
                },
                OnCall = SourceVariableResolver.GetSourceVariable,
                ReturnType = _supplier.Number(),
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
                // = assignment
                new AssignmentOperation(AssignmentOperator.Equal, pipeType, info => info.Set()),
                // += mod append
                new AssignmentOperation(AssignmentOperator.AddEqual, pipeType, info => info.Modify(Operation.AppendToArray)),
                // -= mod remove
                new AssignmentOperation(AssignmentOperator.SubtractEqual, pipeType, info => info.Modify(Operation.RemoveFromArrayByValue))
            });

            ArrayOfType.ArrayHandler.OverrideArray(this);
        }

        ArrayFunctionHandler GetFunctionHandler(ActionSet actionSet)
        {
            // In case the current object is an anonymous array, get the function handler of the real type.
            // For non-anonymous arrays, this is the same thing as 'this.ArrayHandler.GetFunctionHandler()'
            return ((ArrayType)this.GetRealType(actionSet.ThisTypeLinker)).ArrayHandler.GetFunctionHandler();
        }

        /// <summary>Creates a function and adds it to the scope.</summary>
        private void Func(FuncMethodBuilder builder) => Scope.AddNativeMethod(new FuncMethod(builder));

        private GenericSortFunction MakeGenericSortFunction(
            string name, string documentation,
            CodeType returnType, CodeType funcType,
            string parameterDocumentation, Func<ArrayFunctionHandler, ISortFunctionExecutor> pointToExecutor,
            IMethodExtensions methodInfo = null)
        {
            return new GenericSortFunction(name, documentation, parameterDocumentation, returnType, this, funcType, pointToExecutor, methodInfo);
        }

        public override IGettableAssigner GetGettableAssigner(AssigningAttributes attributes)
        {
            var overrideAssigner = ArrayOfType.ArrayHandler.GetArrayAssigner(attributes);
            if (overrideAssigner != null)
                return overrideAssigner;

            return new DataTypeAssigner(attributes);
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, SourceIndexReference reference, VarIndexAssigner assigner)
        {
            var functionHandler = ArrayOfType.ArrayHandler.GetFunctionHandler();
            assigner.Add(_length.Provider, functionHandler.Length(reference.Value));
            assigner.Add(_first.Provider, functionHandler.FirstOf(reference.Value));
            assigner.Add(_last.Provider, functionHandler.LastOf(reference.Value));
        }

        public override AnonymousType[] ExtractAnonymousTypes() => ArrayOfType.ExtractAnonymousTypes();

        // public override bool Implements(CodeType type) => (type is ArrayType arrayType && arrayType.ArrayOfType.Implements(ArrayOfType)) || (ArrayOfType is IAdditionalArray additon && additon.AlternateImplements(type));
        public override Scope GetObjectScope() => Scope;
        protected override bool DoesImplement(CodeType type)
        {
            if (!Attributes.IsStruct && (type is AnyType || ArrayOfType is AnyType))
            {
                return true;
            }
            return type is ArrayType arrayType && arrayType.ArrayOfType.Implements(ArrayOfType);
        }
        public override bool Is(CodeType type) => type is ArrayType other && ArrayOfType.Is(other.ArrayOfType);
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();

        public override string GetName(GetTypeName settings = default(GetTypeName))
        {
            string result = ArrayOfType.GetName(settings);
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
            parseInfo.SourceExpression.OnResolve(expr =>
            {
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