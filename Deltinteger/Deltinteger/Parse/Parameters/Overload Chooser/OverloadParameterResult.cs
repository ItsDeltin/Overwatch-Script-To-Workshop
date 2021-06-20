using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class OverloadParameterResult
    {
        public IExpression Value { get; }
        public object AdditionalData { get; }
        public VariableResolve RefResolvedVariable { get; }
        public DocRange ParameterRange { get; }

        public OverloadParameterResult(IExpression value, object additionalData, VariableResolve refResolvedVariable, DocRange parameterRange)
        {
            Value = value;
            AdditionalData = additionalData;
            RefResolvedVariable = refResolvedVariable;
            ParameterRange = parameterRange;
        }

        public WorkshopParameter ToWorkshop(ActionSet actionSet) => new WorkshopParameter(
            value: Value.Parse(actionSet),
            additionalData: AdditionalData,
            refVariableElements: RefResolvedVariable?.ParseElements(actionSet));
    }

    public class WorkshopParameter
    {
        public IWorkshopTree Value { get; }
        public object AdditionalData { get; }
        public VariableElements RefVariableElements { get; }

        public WorkshopParameter(IWorkshopTree value, object additionalData, VariableElements refVariableElements)
        {
            Value = value;
            AdditionalData = additionalData;
            RefVariableElements = refVariableElements;
        }

        public WorkshopParameter(IWorkshopTree value)
        {
            Value = value;
        }
    }
}