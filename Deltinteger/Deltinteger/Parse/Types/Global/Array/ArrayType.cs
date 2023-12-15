using System;
using System.Collections.Generic;
using System.Linq;
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
                }.AddArrayCopyNotUsedWarning());
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
            }.AddArrayCopyNotUsedWarning());
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

            // Assignment operators
            IEnumerable<AssignmentOperation> assignmentOperations = new AssignmentOperation[] {
                // = assignment
                new(AssignmentOperator.Equal, pipeType, info => info.Set()),
            };
            // Binary operators
            var expressionOperations = Enumerable.Empty<TypeOperation>();

            // Modify Append
            MakeFunctionsForCases(maker =>
            {
                const string APPENDED = "appended";
                const string APPENDING = "appending";
                const string REMOVED = "removed";
                const string REMOVING = "removing";

                // ModAppend
                Func(new FuncMethodBuilder()
                {
                    Name = "ModAppend",
                    Documentation = "Appends a value to the array. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                    Parameters = new[] {
                        maker.CreateValidationParameter("The value that is pushed to the array.", APPENDED, APPENDING),
                    },
                    OnCall = SourceVariableResolver.GetSourceVariable,
                    ReturnType = _supplier.Number(),
                    Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, Operation.AppendToArray, maker.Cast(methodCall.ParameterValues[0]))
                });
                // Append
                Func(new FuncMethodBuilder()
                {
                    Name = "Append",
                    Documentation = "A copy of the array with the specified value appended to it.",
                    ReturnType = this,
                    Parameters = new CodeParameter[] {
                        maker.CreateValidationParameter("The value added to the copied array.", APPENDED, APPENDING)
                    },
                    Action = (actionSet, methodCall) => GetFunctionHandler(actionSet).Append(actionSet.CurrentObject, maker.Cast(methodCall.ParameterValues[0]))
                }.AddArrayCopyNotUsedWarning("ModAppend"));
                // ModRemoveByValue
                Func(new FuncMethodBuilder()
                {
                    Name = "ModRemoveByValue",
                    Documentation = "Removes an element from the array by a value. This will modify the array directly rather than returning a copy of the array. The source expression must be a variable.",
                    Parameters = new CodeParameter[] {
                        maker.CreateValidationParameter("The value that is removed from the array.", REMOVED, REMOVING),
                    },
                    OnCall = SourceVariableResolver.GetSourceVariable,
                    ReturnType = _supplier.Number(),
                    Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, Operation.RemoveFromArrayByValue, maker.Cast(methodCall.ParameterValues[0]))
                });
                // Remove
                Func(new FuncMethodBuilder()
                {
                    Name = "Remove",
                    Documentation = "A copy of the array with the specified value removed from it.",
                    ReturnType = this,
                    Parameters = new CodeParameter[] {
                        maker.CreateValidationParameter("The value that is removed from the array.", REMOVED, REMOVING)
                    },
                    Action = (actionSet, methodCall) => GetFunctionHandler(actionSet).Remove(actionSet.CurrentObject, maker.Cast(methodCall.ParameterValues[0]))
                }.AddArrayCopyNotUsedWarning("ModRemove"));
                // a += b
                assignmentOperations = assignmentOperations.Append(new AssignmentOperation(
                    AssignmentOperator.AddEqual,
                    maker.T,
                    validateParams => maker.ValidateOperation(validateParams, APPENDED, APPENDING),
                    info => info.ModifyWithValueCast(Operation.AppendToArray, maker.Cast)));
                // a -= b
                assignmentOperations = assignmentOperations.Append(new AssignmentOperation(
                    AssignmentOperator.SubtractEqual,
                    maker.T,
                    validateParams => maker.ValidateOperation(validateParams, REMOVED, REMOVING),
                    info => info.ModifyWithValueCast(Operation.RemoveFromArrayByValue, maker.Cast)));
                // 
                // a + b
                // expressionOperations = expressionOperations.Append(new TypeOperation(TypeOperator.Add, maker.T, this,
                //     validateParams => maker.ValidateOperation(validateParams, APPENDED, APPENDING),
                //     (l, r) => Element.Append(l, maker.Cast(r))));
                // a - b
                // expressionOperations = expressionOperations.Append(new TypeOperation(TypeOperator.Subtract, maker.T, this,
                //     validateParams => maker.ValidateOperation(validateParams, REMOVED, REMOVING),
                //     (l, r) => Element.Remove(l, maker.Cast(r))));
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
                Action = (actionSet, methodCall) => SourceVariableResolver.Modify(actionSet, Operation.RemoveFromArrayByIndex, methodCall.ParameterValues[0])
            });

            // Add type operations.
            Operations.AddTypeOperation(assignmentOperations.ToArray());
            Operations.AddTypeOperation(expressionOperations.ToArray());

            ArrayOfType.ArrayHandler.OverrideArray(this);
        }

        ArrayFunctionHandler GetFunctionHandler(ActionSet actionSet)
        {
            // In case the current object is an anonymous array, get the function handler of the real type.
            // For non-anonymous arrays, this is the same thing as 'this.ArrayHandler.GetFunctionHandler()'
            return ((ArrayType)this.GetRealType(actionSet.ThisTypeLinker)).ArrayHandler.GetFunctionHandler();
        }

        readonly struct FunctionCase
        {
            public readonly CodeType T;
            private readonly ArrayType arrayType;
            private readonly Func<IWorkshopTree, IWorkshopTree> cast;

            public FunctionCase(ArrayType arrayType, CodeType t, Func<IWorkshopTree, IWorkshopTree> cast)
            {
                this.arrayType = arrayType;
                this.T = t;
                this.cast = cast;
            }

            public readonly CodeParameter CreateValidationParameter(string valueTask, string operated, string operating)
            {
                var _this = this;
                return new CustomParameterValidation("value", valueTask, T, validate =>
                {
                    _this.CheckExpressionProtection(validate.ParseInfo, validate.ValueRange, validate.Value, operated, operating);
                    return null;
                });
            }

            public readonly void ValidateOperation(ValidateOperationParams validateParams, string operated, string operating)
            {
                CheckExpressionProtection(validateParams.ParseInfo, validateParams.Range, validateParams.Value, operated, operating);
            }

            public readonly void ValidateOperation(ExpressionOperationValidationParams validateParams, string operated, string operating)
            {
                CheckExpressionProtection(validateParams.ParseInfo, validateParams.Range, validateParams.Right, operated, operating);
            }

            private readonly void CheckExpressionProtection(ParseInfo parseInfo, DocRange range, IExpression value, string operated, string operating)
            {
                if (value == null)
                    return;

                var valueType = value.Type();

                if (arrayType.ArrayOfType.NeedsArrayProtection &&
                    value is not MissingElementAction &&
                    !CodeTypeHelpers.IsTypeConfident(valueType))
                {
                    parseInfo.Script.Diagnostics.Warning($"The type '{arrayType.ArrayOfType.GetName()}' is compiled as a workshop array. It is unknown if the righthand being {operated} needs to be wrapped, so this will result in undefined behaviour if the righthand value is a single value rather than an array. Please narrow down the type of the value you are {operating}, or wrap the righthand value in brackets if you know it is supposed to be a single value.", range);
                }
            }

            public readonly IWorkshopTree Cast(IWorkshopTree value) => cast(value);
        }

        private void MakeFunctionsForCases(Action<FunctionCase> factory)
        {
            // T[]
            factory(new(this, this, a => a));
            // T
            factory(new(this, ArrayOfType, a => ToWorkshopHelper.GuardArrayModifiedValue(a, ArrayOfType)));
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

        public static IWorkshopTree Modify(ActionSet actionSet, Operation operation, IWorkshopTree value)
        {
            // var calling = SourceVariableResolver.GetIndexReference(actionSet, methodCall);
            // calling.Modify(actionSet, operation, value: methodCall.ParameterValues[0], target: actionSet.CurrentObjectRelatedIndex.Target);
            // return Element.CountOf(calling.GetVariable());

            actionSet.CurrentObjectRelatedIndex.Reference.Modify(actionSet, operation, value, target: actionSet.CurrentObjectRelatedIndex.Target);
            return (Element)0;
        }
    }
}