using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public interface ICallInfo
    {
        IWorkshopTree[] ParameterValues { get; }
        void ExecuteSubroutine(ActionSet actionSet, Subroutine subroutine);
    }

    class CallInfo : ICallInfo
    {
        public IWorkshopTree[] ParameterValues { get; }

        public CallInfo(IWorkshopTree[] parameters)
        {
            ParameterValues = parameters;
        }

        public CallInfo()
        {
            ParameterValues = new IWorkshopTree[0];
        }

        public void ExecuteSubroutine(ActionSet actionSet, Subroutine subroutine) => Builder.ExecuteSubroutine.Execute(actionSet, subroutine);
    }
}