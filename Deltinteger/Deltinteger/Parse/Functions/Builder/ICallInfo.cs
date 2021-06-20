using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public interface ICallInfo
    {
        WorkshopParameter[] Parameters { get; }
        ReturnHandler ProvidedReturnHandler { get; }
        void ExecuteSubroutine(ActionSet actionSet, Subroutine subroutine);
    }

    class CallInfo : ICallInfo
    {
        public WorkshopParameter[] Parameters { get; }
        public ReturnHandler ProvidedReturnHandler { get; set; }

        public CallInfo(IWorkshopTree[] parameters)
        {
            Parameters = parameters.Select(p => new WorkshopParameter(p)).ToArray();
        }

        public CallInfo() : this(new IWorkshopTree[0]) {}

        public void ExecuteSubroutine(ActionSet actionSet, Subroutine subroutine) => Builder.ExecuteSubroutine.Execute(actionSet, subroutine);
    }
}