using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class IndexReference : IGettable
    {
        public WorkshopArrayBuilder ArrayBuilder { get; set; }
        public WorkshopVariable WorkshopVariable { get; protected set; }
        public Element[] Index { get; protected set; }

        public IndexReference(WorkshopArrayBuilder arrayBuilder, WorkshopVariable workshopVariable, params Element[] index)
        {
            ArrayBuilder = arrayBuilder;
            WorkshopVariable = workshopVariable;
            Index = index;
        }
        protected IndexReference() { }

        public virtual IWorkshopTree GetVariable(Element targetPlayer = null)
        {
            return WorkshopArrayBuilder.GetVariable(targetPlayer ?? Element.EventPlayer(), WorkshopVariable, Index);
        }

        public Element Get(Element targetPlayer = null) => (Element)GetVariable(targetPlayer);

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] index)
            => WorkshopArrayBuilder.SetVariable(ArrayBuilder, value, targetPlayer ?? Element.EventPlayer(), WorkshopVariable, false, ArrayBuilder<Element>.Build(Index, index));

        public virtual Element[] ModifyVariable(Operation operation, IWorkshopTree value, Element targetPlayer = null, params Element[] index)
            => WorkshopArrayBuilder.ModifyVariable(ArrayBuilder, operation, value, targetPlayer ?? Element.EventPlayer(), WorkshopVariable, ArrayBuilder<Element>.Build(Index, index));

        public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, Element[] index)
            => actionSet.AddAction(ModifyVariable(operation, value, target, index));

        public void Set(ActionSet actionSet, IWorkshopTree value, Element target, Element[] index)
        {
            var test = Get(target);
            if (index != null)
                foreach (var i in index)
                    test = Element.ValueInArray(test, i);

            // Do not do anything if the value is assigning to itself.
            if (!test.EqualTo(value))
            {
                actionSet.AddAction(SetVariable((Element)value, target, index));
            }
        }

        public void Set(ActionSet actionSet, Element value, Element target = null, params Element[] index) => Set(actionSet, null, value, target, index);
        public void Set(ActionSet actionSet, string comment, Element value, Element target = null, params Element[] index) => actionSet.AddAction(comment, SetVariable(value, target, index));

        public virtual void Pop(ActionSet actionSet) => throw new Exception("Cannot pop IndexReference");
        public virtual void Push(ActionSet actionSet, IWorkshopTree value) => throw new Exception("Cannot push IndexReference");

        public virtual IndexReference CreateChild(params Element[] index)
        {
            return new IndexReference(ArrayBuilder, WorkshopVariable, [.. Index ?? [], .. index ?? []]);
        }

        IGettable IGettable.ChildFromClassReference(IWorkshopTree reference) => CreateChild((Element)reference);

        public bool CanBeSet() => true;

        public WorkshopVariablePosition? GetWorkshopVariablePosition() => new WorkshopVariablePosition(WorkshopVariable, Index, null);
    }

    public class RecursiveIndexReference : IndexReference
    {
        public RecursiveIndexReference(WorkshopArrayBuilder arrayBuilder, WorkshopVariable workshopVariable, params Element[] index) : base(arrayBuilder, workshopVariable, index)
        {
        }
        public RecursiveIndexReference(IndexReference reference)
        {
            this.WorkshopVariable = reference.WorkshopVariable;
            this.Index = reference.Index;
            this.ArrayBuilder = reference.ArrayBuilder;
        }

        public override IWorkshopTree GetVariable(Element targetPlayer = null)
        {
            return Element.LastOf(base.GetVariable(targetPlayer));
        }

        public override Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] index)
        {
            return base.SetVariable(value, targetPlayer, CurrentIndex(index));
        }

        public override Element[] ModifyVariable(Operation operation, IWorkshopTree value, Element targetPlayer = null, params Element[] index)
        {
            return base.ModifyVariable(operation, value, targetPlayer, CurrentIndex(index));
        }

        public override void Pop(ActionSet actionSet) => actionSet.AddAction(Pop());
        public override void Push(ActionSet actionSet, IWorkshopTree value) => actionSet.AddAction(Push((Element)value));

        public Element[] Reset()
        {
            return base.SetVariable(Element.EmptyArray());
        }

        public Element[] Push(Element value)
        {
            return base.ModifyVariable(Operation.AppendToArray, Element.CreateArray(value));
        }

        public Element[] Pop()
        {
            return base.ModifyVariable(Operation.RemoveFromArrayByIndex, StackLength() - 1);
        }

        private Element[] CurrentIndex(params Element[] setAtIndex)
        {
            return [
                StackLength() - 1,
                ..setAtIndex ?? []
            ];
        }

        private Element StackLength()
        {
            return Element.CountOf(base.GetVariable());
        }

        public override IndexReference CreateChild(params Element[] index)
        {
            return base.CreateChild(CurrentIndex(index));
        }
    }

    public class WorkshopElementReference : IGettable
    {
        public IWorkshopTree WorkshopElement { get; }

        public WorkshopElementReference(IWorkshopTree element)
        {
            WorkshopElement = element;
        }

        public IWorkshopTree GetVariable(Element targetPlayer) => WorkshopElement;

        void Throw() => throw new Exception("Cannot modify WorkshopElementReference");
        public void Set(ActionSet actionSet, IWorkshopTree value, Element target, Element[] index) => Throw();
        public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, Element[] index) => Throw();
        public void Pop(ActionSet actionSet) => Throw();
        public void Push(ActionSet actionSet, IWorkshopTree value) => Throw();
        IGettable IGettable.ChildFromClassReference(IWorkshopTree reference) => new WorkshopElementReference(StructHelper.ValueInArray(WorkshopElement, reference));
        public bool CanBeSet() => false;
        public WorkshopVariablePosition? GetWorkshopVariablePosition() => null;
    }

    // Wraps an IGettable with a known target.
    class TargetGettable : IGettable
    {
        readonly IGettable _parent;
        readonly Element _target;

        public TargetGettable(IGettable parent, Element target) => (_parent, _target) = (parent, target);

        public bool CanBeSet() => _parent.CanBeSet();
        public IGettable ChildFromClassReference(IWorkshopTree reference) => new TargetGettable(_parent.ChildFromClassReference(reference), _target);
        public IWorkshopTree GetVariable(Element eventPlayer = null) => _parent.GetVariable(_target);
        public WorkshopVariablePosition? GetWorkshopVariablePosition()
        {
            var parent = _parent.GetWorkshopVariablePosition();
            return new WorkshopVariablePosition(parent.Value.WorkshopVariable, parent.Value.Index, _target);
        }

        public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, params Element[] index) =>
            _parent.Modify(actionSet, operation, value, _target, index);
        public void Pop(ActionSet actionSet) => _parent.Pop(actionSet);
        public void Push(ActionSet actionSet, IWorkshopTree value) => _parent.Push(actionSet, value);
        public void Set(ActionSet actionSet, IWorkshopTree value, Element target, params Element[] index) => _parent.Set(actionSet, value, _target, index);
    }
}