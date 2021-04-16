using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public interface IParameterHandler
    {
        void Set(ActionSet actionSet, IWorkshopTree[] parameterValues);
        void Push(ActionSet actionSet, IWorkshopTree[] parameterValues);
        void Pop(ActionSet actionSet);
        void AddParametersToAssigner(VarIndexAssigner assigner);
    }
}