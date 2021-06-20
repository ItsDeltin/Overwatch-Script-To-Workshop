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
        void IWorkshopTree.ToWorkshop(WorkshopBuilder language, ToWorkshopContext context) => throw new System.NotImplementedException();
    }

    public class EmptyLambda : IWorkshopTree, ILambdaInvocable
    {
        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) => null;
        bool IWorkshopTree.EqualTo(IWorkshopTree other) => throw new System.NotImplementedException();
        void IWorkshopTree.ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => throw new System.NotImplementedException();
    }
}