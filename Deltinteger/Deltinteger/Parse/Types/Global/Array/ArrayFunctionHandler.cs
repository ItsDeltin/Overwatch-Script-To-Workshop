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
        public virtual IWorkshopTree IndexOf(IWorkshopTree reference, IWorkshopTree value) => Element.IndexOfArrayValue(reference, value);
        public virtual ISortFunctionExecutor SortedArray() => new GeneralSortFunctionExecutor(Element.SORTED_ARRAY);
        public virtual ISortFunctionExecutor FilteredArray() => new GeneralSortFunctionExecutor(Element.FILTERED_ARRAY);
        public virtual ISortFunctionExecutor Any() => new GeneralSortFunctionExecutor(Element.IS_TRUE_FOR_ANY);
        public virtual ISortFunctionExecutor All() => new GeneralSortFunctionExecutor(Element.IS_TRUE_FOR_ALL);
        public virtual ISortFunctionExecutor Map() => new GeneralMapFunctionExecutor();
    }
}