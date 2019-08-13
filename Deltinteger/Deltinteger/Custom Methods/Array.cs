using System;
using Deltin.Deltinteger.Parse;
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
            return new WikiMethod("BlendedIndex", "Allows you to get a value from an array using a non-integer index. Only works on data types that can have math operations performed upon them.", null);
        }
    }

    [CustomMethod("OptimisedBlendedIndex", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    [Parameter("index", ValueType.Number, null)]
    class OptimisedBlendedIndex : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedBlendedIndex: array", TranslateContext.IsGlobal, null);
            IndexedVar index = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedBlendedIndex: index", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable((Element)Parameters[0]),
                index.SetVariable((Element)Parameters[1])
            );

            Element condition = Element.Part<V_Compare>(Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Down)), EnumData.GetEnumValue(Operators.Equal), index.GetVariable());
            Element consequent = Element.Part<V_ValueInArray>(array.GetVariable(), index.GetVariable());
            Element alt1 = Element.Part<V_Multiply>(Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Down))), Element.Part<V_Subtract>(index.GetVariable(), Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Down))));
            Element alt2 = Element.Part<V_Multiply>(Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Up))), Element.Part<V_Subtract>(Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Up)), index.GetVariable()));
            Element alternative = Element.Part<V_Add>(alt1, alt2);

            return new MethodResult(actions, Element.TernaryConditional(condition, consequent, alternative));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("BlendedIndex", "Allows you to get a value from an array using a non-integer index. Only works on data types that can have math operations performed upon them. This method can not be used in a condition.", null);
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

    [CustomMethod("OptimisedRangeOfArray", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    class OptmisedRangeOfArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedRangeOfArray: array", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable((Element)Parameters[0])
            );

            Element min = Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(array.GetVariable(), new V_ArrayElement()));
            Element max = Element.Part<V_LastOf>(Element.Part<V_SortedArray>(array.GetVariable(), new V_ArrayElement()));
            return new MethodResult(actions, Element.Part<V_Subtract>(max, min));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("OptimisedRangeOfArray", "The highest value of an array subtracted by its lowest value. This method does not work in conditions.", null);
        }
    }

    [CustomMethod("SortedMedian", CustomMethodType.Value)]
    [Parameter("array", ValueType.Any, null)]
    class SortedMedian : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = (Element)Parameters[0];
            Element length = Element.Part<V_CountOf>(array);
            Element condition = Element.Part<V_Compare>(Element.Part<V_Modulo>(length, new V_Number(2)), EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element medianIndex = Element.Part<V_Divide>(Element.Part<V_Add>(length, new V_Number(1)), new V_Number(2));
            Element consequent = Element.Part<V_Divide>(Element.Part<V_Add>(Element.Part<V_ValueInArray>(array, Element.Part<V_Subtract>(medianIndex, new V_Number(0.5))), Element.Part<V_ValueInArray>(array, Element.Part<V_Add>(medianIndex, new V_Number(0.5)))), new V_Number(2));
            Element alternative = Element.Part<V_ValueInArray>(array, medianIndex);
            return new MethodResult(null, Element.TernaryConditional(condition, consequent, alternative));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("SortedMedian", "The median of an array that has already been sorted.", null);
        }
    }

    [CustomMethod("OptimisedSortedMedian", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    class OptimisedSortedMedian : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedSortedMedian: array", TranslateContext.IsGlobal, null);
            IndexedVar medianIndex = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedSortedMedian: medianIndex", TranslateContext.IsGlobal, null);

            Element length = Element.Part<V_CountOf>(array.GetVariable());
            Element condition = Element.Part<V_Compare>(Element.Part<V_Modulo>(length, new V_Number(2)), EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element consequent = Element.Part<V_Divide>(Element.Part<V_Add>(Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_Subtract>(medianIndex.GetVariable(), new V_Number(0.5))), Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_Add>(medianIndex.GetVariable(), new V_Number(0.5)))), new V_Number(2));
            Element alternative = Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable());

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable((Element)Parameters[0]),
                medianIndex.SetVariable(Element.Part<V_Divide>(Element.Part<V_Add>(length, new V_Number(1)), new V_Number(2)))
            );
            
            return new MethodResult(actions, Element.TernaryConditional(condition, consequent, alternative));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("OptimisedSortedMedian", "The median of an array that has already been sorted. This method does not work in conditions.", null);
        }
    }

    [CustomMethod("UnsortedMedian", CustomMethodType.Value)]
    [Parameter("array", ValueType.Any, null)]
    class UnsortedMedian : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element array = Element.Part<V_SortedArray>((Element)Parameters[0], new V_ArrayElement());
            Element length = Element.Part<V_CountOf>(array);
            Element condition = Element.Part<V_Compare>(Element.Part<V_Modulo>(length, new V_Number(2)), EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element medianIndex = Element.Part<V_Divide>(Element.Part<V_Add>(length, new V_Number(1)), new V_Number(2));
            Element consequent = Element.Part<V_Divide>(Element.Part<V_Add>(Element.Part<V_ValueInArray>(array, Element.Part<V_Subtract>(medianIndex, new V_Number(0.5))), Element.Part<V_ValueInArray>(array, Element.Part<V_Add>(medianIndex, new V_Number(0.5)))), new V_Number(2));
            Element alternative = Element.Part<V_ValueInArray>(array, medianIndex);
            return new MethodResult(null, Element.TernaryConditional(condition, consequent, alternative));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("UnsortedMedian", "The median of an array that has not been sorted yet.", null);
        }
    }

    [CustomMethod("OptimisedUnsortedMedian", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    class OptimisedUnsortedMedian : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedSortedMedian: array", TranslateContext.IsGlobal, null);
            IndexedVar medianIndex = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedSortedMedian: medianIndex", TranslateContext.IsGlobal, null);

            Element length = Element.Part<V_CountOf>(array.GetVariable());
            Element condition = Element.Part<V_Compare>(Element.Part<V_Modulo>(length, new V_Number(2)), EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element consequent = Element.Part<V_Divide>(Element.Part<V_Add>(Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_Subtract>(medianIndex.GetVariable(), new V_Number(0.5))), Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_Add>(medianIndex.GetVariable(), new V_Number(0.5)))), new V_Number(2));
            Element alternative = Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable());

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable(Element.Part<V_SortedArray>((Element)Parameters[0], new V_ArrayElement())),
                medianIndex.SetVariable(Element.Part<V_Divide>(Element.Part<V_Add>(length, new V_Number(1)), new V_Number(2)))
            );

            return new MethodResult(actions, Element.TernaryConditional(condition, consequent, alternative));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("OptimisedUnsortedMedian", "The median of an array that has not been sorted yet. This method does not work in conditions.", null);
        }
    }
}