using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ArrayFunctionHandler
    {
        public virtual void AssignLength(IVariable lengthVariable, VarIndexAssigner assigner, IWorkshopTree reference) => assigner.Add(lengthVariable, Element.CountOf(reference));
        public virtual void AssignFirstOf(IVariable firstOfVariable, VarIndexAssigner assigner, IWorkshopTree reference) => assigner.Add(firstOfVariable, Element.FirstOf(reference));
        public virtual void AssignLastOf(IVariable lastOfVariable, VarIndexAssigner assigner, IWorkshopTree reference) => assigner.Add(lastOfVariable, Element.LastOf(reference));
        public virtual ISortFunctionExecutor SortedArray() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor FilteredArray() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor Any() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor All() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor Map() => new GeneralSortFunctionExecutor();
    }
}