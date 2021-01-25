using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("HorizontalDistance", CustomMethodType.Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    class HorizontalDistance : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            Element x = Element.XOf(point1) - Element.XOf(point2);
            Element z = Element.ZOf(point1) - Element.ZOf(point2);
            Element sum = Element.Part<V_RaiseToPower>(x, Element.Num(2)) + Element.Part<V_RaiseToPower>(z, Element.Num(2));
            return new MethodResult(null, Element.Part<V_SquareRoot>(sum));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The distance between 2 points as if they were on the same Y level.",
                // Parameters
                "The first point.",
                "The second point."
            );
        }
    }

    [CustomMethod("OptimisedHorizontalDistance", CustomMethodType.MultiAction_Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    class OptimisedHorizontalDistance : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar point1 = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedHorizontalDistance: point1", TranslateContext.IsGlobal);
            IndexedVar point2 = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "OptimisedHorizontalDistance: point2", TranslateContext.IsGlobal);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                point1.SetVariable((Element)Parameters[0]),
                point2.SetVariable((Element)Parameters[1])
            );

            Element x = Element.XOf(point1.GetVariable()) - Element.XOf(point2.GetVariable());
            Element z = Element.ZOf(point1.GetVariable()) - Element.ZOf(point2.GetVariable());
            Element sum = Element.Part<V_RaiseToPower>(x, Element.Num(2)) + Element.Part<V_RaiseToPower>(z, Element.Num(2));

            return new MethodResult(actions, Element.Part<V_SquareRoot>(sum));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The distance between 2 points as if they were on the same Y level.",
                // Parameters
                "The first point.",
                "The second point."
            );
        }
    }
}
