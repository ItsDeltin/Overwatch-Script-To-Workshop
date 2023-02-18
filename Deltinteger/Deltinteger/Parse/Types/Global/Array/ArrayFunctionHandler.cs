using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Parse
{
    public class ArrayFunctionHandler
    {
        public bool AllowUnhandled { get; protected set; } = true;

        public virtual IWorkshopTree Length(IWorkshopTree reference) => Element.CountOf(reference);
        public virtual IWorkshopTree FirstOf(IWorkshopTree reference) => Element.FirstOf(reference);
        public virtual IWorkshopTree LastOf(IWorkshopTree reference) => Element.LastOf(reference);
        public virtual IWorkshopTree Contains(IWorkshopTree reference, IWorkshopTree value) => Element.Contains(reference, value);
        public virtual IWorkshopTree Append(IWorkshopTree reference, IWorkshopTree value) => Element.Append(reference, value);
        public virtual IWorkshopTree Slice(IWorkshopTree reference, IWorkshopTree start, IWorkshopTree count) => Element.Slice(reference, start, count);
        public virtual ISortFunctionExecutor SortedArray() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor FilteredArray() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor Any() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor All() => new GeneralSortFunctionExecutor();
        public virtual ISortFunctionExecutor Map() => new GeneralSortFunctionExecutor();
    }
}