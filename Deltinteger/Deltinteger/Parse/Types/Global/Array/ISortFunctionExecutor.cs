using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public interface ISortFunctionExecutor
    {
        IWorkshopTree GetResult(ActionSet actionSet, Func<IWorkshopTree, IWorkshopTree> invoke);
    }

    class GeneralSortFunctionExecutor : ISortFunctionExecutor
    {
        readonly string function;

        public GeneralSortFunctionExecutor(string function) => this.function = function;

        public IWorkshopTree GetResult(
            ActionSet actionSet,
            Func<IWorkshopTree, IWorkshopTree> invoke)
            => Element.Part(function, actionSet.CurrentObject, invoke(Element.ArrayElement()));
    }

    class GeneralMapFunctionExecutor : ISortFunctionExecutor
    {
        public IWorkshopTree GetResult(ActionSet actionSet, Func<IWorkshopTree, IWorkshopTree> invoke)
        {
            // Get the lambda value.
            var mapValue = invoke(Element.ArrayElement());

            // If a normal workshop value is being mapped into a struct, then bridge the struct value.
            if (mapValue is IStructValue structValue)
                return structValue.Bridge(bargs => Element.Map(actionSet.CurrentObject, bargs.Value));

            // Normal map.
            return Element.Map(actionSet.CurrentObject, mapValue);
        }
    }
}