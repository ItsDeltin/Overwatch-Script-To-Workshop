using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Parse
{
    public class ArrayFunctionHandler
    {
        public virtual IWorkshopTree Length(IWorkshopTree reference) => CountOf(reference);
        public virtual IWorkshopTree FirstOf(IWorkshopTree reference) => FirstOf(reference);
        public virtual IWorkshopTree LastOf(IWorkshopTree reference) => LastOf(reference);
        public virtual IWorkshopTree Contains(IWorkshopTree reference, IWorkshopTree value) => Contains(reference, value);
        public virtual ISortFunctionExecutor SortedArray() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor FilteredArray() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor Any() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor All() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor Map() => new GeneralSortFunctionExecutor();
    }
}