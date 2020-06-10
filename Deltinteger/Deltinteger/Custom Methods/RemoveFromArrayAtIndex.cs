using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("RemoveFromArrayAtIndex", "Removes a value from an array by its index.", CustomMethodType.Value)]
    class RemoveFromArrayAtIndex : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("array", "The array to modify."),
                new CodeParameter("index", "The index to remove.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            Element array = (Element)parameters[0];
            Element index = (Element)parameters[1];

            return Element.Part<V_Append>(
                Element.Part<V_ArraySlice>(array, new V_Number(0), index),
                Element.Part<V_ArraySlice>(array, index + 1, V_Number.LargeArbitraryNumber)
            );
        }
    }
}