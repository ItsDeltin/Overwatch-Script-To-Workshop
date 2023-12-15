using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Lambda;

namespace Deltin.Deltinteger.Parse
{
    public class GenericSortFunction
    {
        readonly string name; // The name of the created ostw function.
        readonly string documentation; // The documentation of the ostw function.
        readonly string parameterDocumentation; // The description of the lambda parameter.
        readonly CodeType returnType; // The return type of the ostw function.
        readonly ArrayType arrayType; // The array type that this belongs to.
        readonly CodeType funcType; // The type that the lambda parameter returns.
        readonly Func<ArrayFunctionHandler, ISortFunctionExecutor> pointToExecutor; // Gets the respective ISortFunctionExecutor from the ArrayFunctionHandler.
        readonly IMethodExtensions methodInfo; // Additional function info.

        public GenericSortFunction(
            string name,
            string documentation,
            string parameterDocumentation,
            CodeType returnType,
            ArrayType arrayType,
            CodeType funcType,
            Func<ArrayFunctionHandler, ISortFunctionExecutor> pointToExecutor,
            IMethodExtensions methodInfo = null)
        {
            this.name = name;
            this.documentation = documentation;
            this.parameterDocumentation = parameterDocumentation;
            this.returnType = returnType;
            this.arrayType = arrayType;
            this.funcType = funcType;
            this.pointToExecutor = pointToExecutor;
            this.methodInfo = methodInfo;
        }

        public void Add(Scope addToScope, ITypeSupplier supplier)
        {
            // value => ...
            var noIndex = GetFuncMethod();
            noIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", parameterDocumentation, PortableLambdaType.CreateConstantType(funcType, arrayType.ArrayOfType))
            };
            noIndex.Action = (actionSet, methodCall) =>
                GetExecutor(actionSet).GetResult(actionSet, inv => Lambda(methodCall).Invoke(actionSet, inv));

            // (value, index) => ...
            var withIndex = GetFuncMethod();
            withIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", parameterDocumentation, PortableLambdaType.CreateConstantType(funcType, arrayType.ArrayOfType, supplier.Number()))
            };
            withIndex.Action = (actionSet, methodCall) =>
                GetExecutor(actionSet).GetResult(actionSet, inv => Lambda(methodCall).Invoke(actionSet, inv, Element.ArrayIndex()));

            addToScope.AddNativeMethod(new FuncMethod(noIndex.AddArrayCopyNotUsedWarning()));
            addToScope.AddNativeMethod(new FuncMethod(withIndex.AddArrayCopyNotUsedWarning()));
        }

        ISortFunctionExecutor GetExecutor(ActionSet actionSet) => pointToExecutor(arrayType.GetRealType(actionSet.ThisTypeLinker).ArrayHandler.GetFunctionHandler());

        static ILambdaInvocable Lambda(MethodCall methodCall) => (ILambdaInvocable)methodCall.ParameterValues[0];

        private FuncMethodBuilder GetFuncMethod() => new FuncMethodBuilder()
        {
            Name = name,
            Documentation = documentation,
            ReturnType = returnType,
            MethodInfo = methodInfo
        };
    }
}