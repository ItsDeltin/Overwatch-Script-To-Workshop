using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("LinearInterpolate", CustomMethodType.Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    [Parameter("fraction", ValueType.Number, null)]
    class LinearInterpolate : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            Element fraction = (Element)Parameters[2];
            Element p1 = Element.Part<V_Multiply>(point1, Element.Part<V_Subtract>(new V_Number(1), fraction));
            Element p2 = Element.Part<V_Multiply>(point2, fraction);

            return new MethodResult(null, Element.Part<V_Add>(p1, p2));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("LinearInterpolate", "A point a fraction along the distance between 2 points.", null);
        }
    }

    [CustomMethod("OptimisedLinearInterpolate", CustomMethodType.MultiAction_Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    [Parameter("fraction", ValueType.Number, null)]
    class OptimisedLinearInterpolate : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar fraction = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedLinearInterpolate: fraction", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                fraction.SetVariable((Element)Parameters[2])
            );

            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            Element p1 = Element.Part<V_Multiply>(point1, Element.Part<V_Subtract>(new V_Number(1), fraction.GetVariable()));
            Element p2 = Element.Part<V_Multiply>(point2, fraction.GetVariable());

            return new MethodResult(actions, Element.Part<V_Add>(p1, p2));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("OptimisedLinearInterpolate", "A point a fraction along the distance between 2 points.", null);
        }
    }

    [CustomMethod("LinearInterpolateDistance", CustomMethodType.Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    [Parameter("distance", ValueType.Number, null)]
    class LinearInterpolateDistance : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            Element distance = (Element)Parameters[2];
            Element fraction = Element.Part<V_Divide>(distance, Element.Part<V_DistanceBetween>(point1, point2));
            Element p1 = Element.Part<V_Multiply>(point1, Element.Part<V_Subtract>(new V_Number(1), fraction));
            Element p2 = Element.Part<V_Multiply>(point2, fraction);
            return new MethodResult(null, Element.Part<V_Add>(p1, p2));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("LinearInterpolateDistance", "A point a distance along a straight line between 2 points.", null);
        }
    }
}
