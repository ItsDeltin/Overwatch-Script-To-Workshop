using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Functions.Builder;

namespace Deltin.Deltinteger
{
    public class MethodCall : Deltin.Deltinteger.Parse.Functions.Builder.ICallInfo
    {
        public IWorkshopTree[] ParameterValues => Parameters.Select(p => p.Value).ToArray();
        public object[] AdditionalParameterData => Parameters.Select(p => p.AdditionalData).ToArray();

        public WorkshopParameter[] Parameters { get; }

        public object AdditionalData { get; set; }
        public InstanceAnonymousTypeLinker TypeArgs { get; set; }
        public CallParallel ParallelMode { get; set; } = CallParallel.NoParallel;
        public string ActionComment { get; set; }
        public ReturnHandler ProvidedReturnHandler { get; set; }

        public MethodCall(WorkshopParameter[] parameters)
        {
            Parameters = parameters;
        }

        public MethodCall(IWorkshopTree[] parameterValues)
        {
            Parameters = parameterValues.Select(p => new WorkshopParameter(p, null, null)).ToArray();
        }

        /// <summary>Gets a parameter as an element.</summary>
        public Element Get(int i) => (Element)ParameterValues[i];

        void ICallInfo.ExecuteSubroutine(ActionSet actionSet, Subroutine subroutine) => ExecuteSubroutine.Execute(actionSet, subroutine, ParallelMode);
    }
}