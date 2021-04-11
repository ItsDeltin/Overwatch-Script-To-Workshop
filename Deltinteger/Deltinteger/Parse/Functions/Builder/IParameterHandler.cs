using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public interface IParameterHandlerProvider
    {
        IParameterHandler CreateInstance();
    }

    public interface IParameterHandler
    {
        void Set(ActionSet actionSet, IWorkshopTree value);
        void Push(ActionSet actionSet, IWorkshopTree value);
        void Pop(ActionSet actionSet);
        void AddToAssigner(VarIndexAssigner assigner);
    }

    class ParameterHandler : IParameterHandler
    {
        protected readonly IGettable _gettable;

        public ParameterHandler(IGettable gettable)
        {
            _gettable = gettable;
        }

        public void Set(ActionSet actionSet, IWorkshopTree value) => _gettable.Set(actionSet, value);

        public void Pop(ActionSet actionSet)
        {
            if (_gettable is RecursiveIndexReference recursive)
                actionSet.AddAction(recursive.Pop());
            else
                throw new Exception("Cannot pop non-recursive parameter");
        }

        public void Push(ActionSet actionSet, IWorkshopTree value)
        {
            if (_gettable is RecursiveIndexReference recursive)
                actionSet.AddAction(recursive.Push((Element)value));
            else
                throw new Exception("Cannot push non-recursive parameter");
        }

        public virtual void AddToAssigner(VarIndexAssigner assigner) {}
    }
}