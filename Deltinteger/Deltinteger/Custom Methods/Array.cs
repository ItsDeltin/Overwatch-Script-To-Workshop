using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("BlendedIndex", CustomMethodType.Value)]
    [Parameter("array", ValueType.Any, null)]
    [Parameter("index", ValueType.Number, null)]
    class BlendedIndex : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element index = (Element)Parameters[1];
            Element condition = Element.Part<V_Compare>(Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Down)), EnumData.GetEnumValue(Operators.Equal), index);
            Element consequent = Element.Part<V_ValueInArray>(array, index);
            Element alt1 = Element.Part<V_Multiply>(Element.Part<V_ValueInArray>(array, Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Down))), Element.Part<V_Subtract>(index, Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Down))));
            Element alt2 = Element.Part<V_Multiply>(Element.Part<V_ValueInArray>(array, Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Up))), Element.Part<V_Subtract>(Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Up)), index));
            Element alternative = Element.Part<V_Add>(alt1, alt2);
            return new MethodResult(null, Element.TernaryConditional(condition, consequent, alternative));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("BlendedIndex", "Allows you to get a value from an array using a non-integer index. Only works on data types that can have math operation performed upon them.", null);
        }
    }

    [CustomMethod("MinOfArray", CustomMethodType.Value)]
    [Parameter("array", ValueType.Any, null)]
    class MinOfArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element min = Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(array, new V_ArrayElement()));
            return new MethodResult(null, min);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("MinOfArray", "The lowest value of an array.", null);
        }
    }

    [CustomMethod("MaxOfArray", CustomMethodType.Value)]
    [Parameter("array", ValueType.Any, null)]
    class MaxOfArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element max = Element.Part<V_LastOf>(Element.Part<V_SortedArray>(array, new V_ArrayElement()));
            return new MethodResult(null, max);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("MaxOfArray", "The highest value of an array.", null);
        }
    }

    [CustomMethod("RangeOfArray", CustomMethodType.Value)]
    [Parameter("array", ValueType.Any, null)]
    class RangeOfArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element min = Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(array, new V_ArrayElement()));
            Element max = Element.Part<V_LastOf>(Element.Part<V_SortedArray>(array, new V_ArrayElement()));
            return new MethodResult(null, Element.Part<V_Subtract>(max, min));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("RangeOfArray", "The highest value of an array subtracted by its lowest value.", null);
        }
    }
}