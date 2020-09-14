﻿using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("Pythag", CustomMethodType.Value)]
    [Parameter("side1", ValueType.Number, null)]
    [Parameter("side2", ValueType.Number, null)]
    class Pythag : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element side1 = (Element)Parameters[0];
            Element side2 = (Element)Parameters[1];
            Element s1 = Element.Part<V_RaiseToPower>(side1, Element.Num(2));
            Element s2 = Element.Part<V_RaiseToPower>(side2, Element.Num(2));
            Element sum = s1 + s2;
            return new MethodResult(null, Element.Part<V_SquareRoot>(sum));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Calculates the length of the longest side (hypotenuse) of a right angled triangle.",
                // Parameters
                "The first side.",
                "The second side."
            );
        }
    }

    [CustomMethod("PythagConverse", CustomMethodType.Value)]
    [Parameter("side1", ValueType.Number, null)]
    [Parameter("hypotenuse", ValueType.Number, null)]
    class PythagConverse : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element side1 = (Element)Parameters[0];
            Element side2 = (Element)Parameters[1];
            Element s1 = Element.Part<V_RaiseToPower>(side1, Element.Num(2));
            Element s2 = Element.Part<V_RaiseToPower>(side2, Element.Num(2));
            Element sum = s2 - s1;
            return new MethodResult(null, Element.Part<V_SquareRoot>(sum));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Calculates the missing side of a right angled triangle.",
                // Parameters
                "The first side.",
                "The hypotenuse."
            );
        }
    }
}
