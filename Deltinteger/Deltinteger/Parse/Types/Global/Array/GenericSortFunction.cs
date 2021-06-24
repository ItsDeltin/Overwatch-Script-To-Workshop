using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Lambda;

namespace Deltin.Deltinteger.Parse
{
    public class GenericSortFunction
    {
        public string Name;
        public string Documentation;
        public string ParameterDocumentation;
        public CodeType ReturnType;
        public CodeType ArrayOfType;
        public CodeType FuncType;
        public string Function;
        public ISortFunctionExecutor Executor = new GeneralSortFunctionExecutor();


        public void Add(Scope addToScope, ITypeSupplier supplier)
        {
            // value => ...
            var noIndex = GetFuncMethod();
            noIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", ParameterDocumentation, PortableLambdaType.CreateConstantType(FuncType, ArrayOfType))
            };
            noIndex.Action = (actionSet, methodCall) =>
                Executor.GetResult(Function, actionSet, inv => Lambda(methodCall).Invoke(actionSet, inv));

            // (value, index) => ...
            var withIndex = GetFuncMethod();
            withIndex.Parameters = new CodeParameter[] {
                new CodeParameter("conditionLambda", ParameterDocumentation, PortableLambdaType.CreateConstantType(FuncType, ArrayOfType, supplier.Number()))
            };
            withIndex.Action = (actionSet, methodCall) =>
                Executor.GetResult(Function, actionSet, inv => Lambda(methodCall).Invoke(actionSet, inv, Element.ArrayIndex()));
            
            addToScope.AddNativeMethod(new FuncMethod(noIndex));
            addToScope.AddNativeMethod(new FuncMethod(withIndex));
        }

        static ILambdaInvocable Lambda(MethodCall methodCall) => (ILambdaInvocable)methodCall.ParameterValues[0];

        private FuncMethodBuilder GetFuncMethod() => new FuncMethodBuilder()
        {
            Name = Name,
            Documentation = Documentation,
            ReturnType = ReturnType
        };
    }

    public interface ISortFunctionExecutor
    {
        IWorkshopTree GetResult(string function, ActionSet actionSet, Func<IWorkshopTree, IWorkshopTree> invoke);
    }

    class GeneralSortFunctionExecutor : ISortFunctionExecutor
    {
        public virtual IWorkshopTree GetResult(
            string function,
            ActionSet actionSet,
            Func<IWorkshopTree, IWorkshopTree> invoke)
            => Element.Part(function, actionSet.CurrentObject, invoke(Element.ArrayElement()));
    }
}