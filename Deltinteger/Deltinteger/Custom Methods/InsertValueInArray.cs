using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("InsertValueInArray", CustomMethodType.Value)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Index", ValueType.Number, null)]
    [Parameter("Value", ValueType.Any, null)]
    class InsertValueInArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element index = (Element)Parameters[1];
            Element value = (Element)Parameters[2];

            return new MethodResult(null,
                Element.Part<V_Append>(
                    Element.Part<V_Append>(
                        Element.Part<V_ArraySlice>(array, new V_Number(0), index),
                        value
                    ),
                    Element.Part<V_ArraySlice>(array, index, V_Number.LargeArbitraryNumber)
                )
            );
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Inserts a value into an array.",
                // Parameters
                "The array to modify.",
                "Where to insert the value.",
                "The value to insert."
            );
        }
    }
}