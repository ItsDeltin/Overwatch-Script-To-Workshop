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
            return WorkshopArrayBuilder.GetVariable(targetPlayer, WorkshopVariable, Index);
        }

        public Element Get(Element targetPlayer = null) => (Element)GetVariable(targetPlayer);

        public virtual Element[] SetVariable(Element value, Element targetPlayer = null, params Element[] index)
        {
            return WorkshopArrayBuilder.SetVariable(ArrayBuilder, value, targetPlayer, WorkshopVariable, false, ArrayBuilder<Element>.Build(Index, index));
        }

        public virtual Element[] ModifyVariable(Operation operation, Element value, Element targetPlayer = null, params Element[] index)
        {
            return WorkshopArrayBuilder.ModifyVariable(ArrayBuilder, operation, value, targetPlayer, WorkshopVariable, ArrayBuilder<Element>.Build(Index, index));
        }

        public IndexReference CreateChild(params Element[] index)
        {
            // Note: `ArrayBuilder` and `ArrayBuilder<Element>` are 2 very different things.
            return new IndexReference(ArrayBuilder, WorkshopVariable, ArrayBuilder<Element>.Build(Index, index));
        }
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
            return base.SetVariable(value, targetPlayer, CurrentIndex(targetPlayer, index));
        }

        public override Element[] ModifyVariable(Operation operation, Element value, Element targetPlayer = null, params Element[] index)
        {
            return base.ModifyVariable(operation, value, targetPlayer, CurrentIndex(targetPlayer, index));
        }

        public Element[] Reset()
        {
            return base.SetVariable(Element.EmptyArray());
        }

        public Element[] Push(Element value)
        {
            return base.ModifyVariable(Operation.AppendToArray, value);
        }

        public Element[] Pop()
        {
            return base.ModifyVariable(Operation.RemoveFromArrayByIndex, StackLength() - 1);
        }

        private Element[] CurrentIndex(Element targetPlayer, params Element[] setAtIndex)
        {
            return ArrayBuilder<Element>.Build(
                StackLength() - 1,
                setAtIndex
            );
        }

        private Element StackLength()
        {
            return Element.CountOf(base.GetVariable());
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
    }
}