using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class VariableElements
    {
        public IGettable IndexReference { get; }
        public Element Target { get; }

        public VariableElements(IGettable indexReference, Element target)
        {
            IndexReference = indexReference;
            Target = target;
        }

        public IGettable Childify()
        {
            return Target == null ? IndexReference : new TargetGettable(IndexReference, Target);
        }

        public WorkshopVariablePosition GetWorkshopVariablePosition()
        {
            var pos = IndexReference.GetWorkshopVariablePosition();
            return new(pos.Value.WorkshopVariable, pos.Value.Index, pos.Value.Target ?? Target ?? Element.EventPlayer());
        }
    }
}