using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class LambdaActionWorkshopInstance : IWorkshopTree, ILambdaInvocable
    {
        public VarIndexAssigner Assigner { get; }
        public LambdaAction Action { get; }

        public LambdaActionWorkshopInstance(ActionSet actionSet, LambdaAction action)
        {
            Assigner = actionSet.IndexAssigner;
            Action = action;
        }

        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues)
            => Action.Invoke(Assigner, actionSet, parameterValues);

        bool IWorkshopTree.EqualTo(IWorkshopTree other) => throw new System.NotImplementedException();
        string IWorkshopTree.ToWorkshop(OutputLanguage language, ToWorkshopContext context) => throw new System.NotImplementedException();
    }
}