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
        readonly string alias; // An extra name for the function.
        readonly IMethodExtensions methodInfo; // Additional function info.

        public GenericSortFunction(
            string name,
            string documentation,
            string parameterDocumentation,
            CodeType returnType,
            ArrayType arrayType,
            CodeType funcType,
            Func<ArrayFunctionHandler, ISortFunctionExecutor> pointToExecutor,
            string alias = null,
            IMethodExtensions methodInfo = null)
        {
            this.name = name;
            this.documentation = documentation;
            this.parameterDocumentation = parameterDocumentation;
            this.returnType = returnType;
            this.arrayType = arrayType;
            this.funcType = funcType;
            this.pointToExecutor = pointToExecutor;
            this.alias = alias;
            this.methodInfo = methodInfo;
        }

        public void Add(Scope addToScope, ITypeSupplier supplier)
        {
            AddWithName(addToScope, supplier, name);
            if (alias is not null)
                AddWithName(addToScope, supplier, alias);
        }

        void AddWithName(Scope addToScope, ITypeSupplier supplier, string withName)
        {
            // value => ...
            var noIndex = GetFuncMethod(withName);
            noIndex.Parameters = [
                new CodeParameter("conditionLambda", parameterDocumentation, PortableLambdaType.CreateConstantType(funcType, arrayType.ArrayOfType))
            ];
            noIndex.Action = (actionSet, methodCall) =>
                GetExecutor(actionSet).GetResult(actionSet, inv => Lambda(methodCall).Invoke(actionSet, inv));

            // (value, index) => ...
            var withIndex = GetFuncMethod(withName);
            withIndex.Parameters = [
                new CodeParameter("conditionLambda", parameterDocumentation, PortableLambdaType.CreateConstantType(funcType, arrayType.ArrayOfType, supplier.Number()))
            ];
            withIndex.Action = (actionSet, methodCall) =>
                GetExecutor(actionSet).GetResult(actionSet, inv => Lambda(methodCall).Invoke(actionSet, inv, Element.ArrayIndex()));

            addToScope.AddNativeMethod(new FuncMethod(noIndex.AddArrayCopyNotUsedWarning()));
            addToScope.AddNativeMethod(new FuncMethod(withIndex.AddArrayCopyNotUsedWarning()));
        }

        private FuncMethodBuilder GetFuncMethod(string withName) => new FuncMethodBuilder()
        {
            Name = withName,
            Documentation = documentation,
            ReturnType = returnType,
            MethodInfo = methodInfo
        };

        ISortFunctionExecutor GetExecutor(ActionSet actionSet) => pointToExecutor(arrayType.GetRealType(actionSet.ThisTypeLinker).ArrayHandler.GetFunctionHandler());

        static ILambdaInvocable Lambda(MethodCall methodCall) => (ILambdaInvocable)methodCall.ParameterValues[0];
    }
}