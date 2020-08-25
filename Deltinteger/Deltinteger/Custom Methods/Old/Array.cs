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
            Element alt1 = Element.Part<V_ValueInArray>(array, Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Down))) * (index - Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Down)));
            Element alt2 = Element.Part<V_ValueInArray>(array, Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Up))) * (Element.Part<V_RoundToInteger>(index, EnumData.GetEnumValue(Rounding.Up)) - index);
            Element alternative = alt1 + alt2;

            return new MethodResult(null, Element.TernaryConditional(condition, consequent, alternative, false));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Allows you to get a value from an array using a non-integer index. Only works on data types that can have math operations performed upon them.",
                "Input Array",
                "Index to access. Can be a non-integer."
            );
        }
    }

    [CustomMethod("OptimisedBlendedIndex", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    [Parameter("index", ValueType.Number, null)]
    class OptimisedBlendedIndex : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedBlendedIndex: array", TranslateContext.IsGlobal);
            IndexedVar index = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedBlendedIndex: index", TranslateContext.IsGlobal);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable((Element)Parameters[0]),
                index.SetVariable((Element)Parameters[1])
            );

            Element condition = Element.Part<V_Compare>(Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Down)), EnumData.GetEnumValue(Operators.Equal), index.GetVariable());
            Element consequent = Element.Part<V_ValueInArray>(array.GetVariable(), index.GetVariable());
            Element alt1 = Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Down))) * Element.Part<V_Subtract>(index.GetVariable(), Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Down)));
            Element alt2 = Element.Part<V_ValueInArray>(array.GetVariable(), Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Up))) * (Element.Part<V_RoundToInteger>(index.GetVariable(), EnumData.GetEnumValue(Rounding.Up)) - index.GetVariable());
            Element alternative = alt1 + alt2;

            return new MethodResult(actions, Element.TernaryConditional(condition, consequent, alternative, false));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Allows you to get a value from an array using a non-integer index. Only works on data types that can have math operations performed upon them.",
                "Input Array",
                "Index to access. Can be a non-integer."
            );
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

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The lowest value of an array.",
                // Parameters
                "The array to get the lowest value from."
            );
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

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The highest value of an array.",
                // Parameters
                "The array to get the highest value from."
            );
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
            return new MethodResult(null, max - min);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The highest value of an array subtracted by its lowest value.",
                // Parameters
                "The array to get the range from."
            );
        }
    }

    [CustomMethod("OptimisedRangeOfArray", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    class OptmisedRangeOfArray : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedRangeOfArray: array", TranslateContext.IsGlobal);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable((Element)Parameters[0])
            );

            Element min = Element.Part<V_FirstOf>(Element.Part<V_SortedArray>(array.GetVariable(), new V_ArrayElement()));
            Element max = Element.Part<V_LastOf>(Element.Part<V_SortedArray>(array.GetVariable(), new V_ArrayElement()));
            return new MethodResult(actions, max - min);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The highest value of an array subtracted by its lowest value.",
                // Parameters
                "The array to get the range from."
            );
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
            Element condition = Element.Part<V_Compare>(length % 2, EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element medianIndex = (length + 1) / 2;
            Element consequent = (Element.Part<V_ValueInArray>(array, medianIndex - 0.5) + Element.Part<V_ValueInArray>(array, medianIndex + 0.5)) / 2;
            Element alternative = Element.Part<V_ValueInArray>(array, medianIndex);
            return new MethodResult(null, Element.TernaryConditional(condition, consequent, alternative, false));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The median of an array that has already been sorted.",
                // Parameters
                "The array to get the median from."
            );
        }
    }

    [CustomMethod("OptimisedSortedMedian", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    class OptimisedSortedMedian : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedSortedMedian: array", TranslateContext.IsGlobal);
            IndexedVar medianIndex = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedSortedMedian: medianIndex", TranslateContext.IsGlobal);

            Element length = Element.Part<V_CountOf>(array.GetVariable());
            Element condition = Element.Part<V_Compare>(length % 2, EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element consequent = (Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable() - 0.5) + Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable() + 0.5)) / 2;
            Element alternative = Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable());

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable((Element)Parameters[0]),
                medianIndex.SetVariable((length + 1) / 2)
            );
            
            return new MethodResult(actions, Element.TernaryConditional(condition, consequent, alternative, false));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The median of an array that has already been sorted.",
                // Parameters
                "The array to get the median from."
            );
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
            Element condition = Element.Part<V_Compare>(length % 2, EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element medianIndex = (length + 1) / 2;
            Element consequent = (Element.Part<V_ValueInArray>(array, medianIndex - 0.5) + Element.Part<V_ValueInArray>(array, medianIndex + 0.5)) / 2;
            Element alternative = Element.Part<V_ValueInArray>(array, medianIndex);
            return new MethodResult(null, Element.TernaryConditional(condition, consequent, alternative, false));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The median of an array that has not been sorted yet.",
                // Parameters
                "The array to get the median from."
            );
        }
    }

    [CustomMethod("OptimisedUnsortedMedian", CustomMethodType.MultiAction_Value)]
    [Parameter("array", ValueType.Any, null)]
    class OptimisedUnsortedMedian : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar array = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedSortedMedian: array", TranslateContext.IsGlobal);
            IndexedVar medianIndex = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedSortedMedian: medianIndex", TranslateContext.IsGlobal);

            Element length = Element.Part<V_CountOf>(array.GetVariable());
            Element condition = Element.Part<V_Compare>(length % 2, EnumData.GetEnumValue(Operators.Equal), new V_Number(0));
            Element consequent = (Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable() - 0.5) + Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable() + 0.5)) / 2;
            Element alternative = Element.Part<V_ValueInArray>(array.GetVariable(), medianIndex.GetVariable());

            Element[] actions = ArrayBuilder<Element>.Build
            (
                array.SetVariable(Element.Part<V_SortedArray>((Element)Parameters[0], new V_ArrayElement())),
                medianIndex.SetVariable((length + 1) / 2)
            );

            return new MethodResult(actions, Element.TernaryConditional(condition, consequent, alternative, false));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The median of an array that has not been sorted yet.",
                // Parameters
                "The array to get the median from."
            );
        }
    }
}