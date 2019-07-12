using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("RemoveFromArrayAtIndex", CustomMethodType.Value)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Index", ValueType.Number, null)]
    class RemoveFromArrayAtIndex : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element index = (Element)Parameters[1];

            return new MethodResult(null, 
                Element.Part<V_Append>(
                    Element.Part<V_ArraySlice>(array, new V_Number(0), index),
                    Element.Part<V_ArraySlice>(array, Element.Part<V_Add>(index, new V_Number(1)), V_Number.LargeArbitraryNumber)
                )
            );
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("RemoveFromArrayAtIndex", "Removes a value from an array by it's index.",
                new WikiParameter[]
                {
                    new WikiParameter("Array", "The array to modify."),
                    new WikiParameter("Index", "The index to remove.")
                }
            );
        }
    }
}