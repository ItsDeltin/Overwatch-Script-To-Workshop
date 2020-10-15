using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("Midpoint", "The midpoint between 2 vectors.", CustomMethodType.Value, typeof(VectorType))]
    class Midpoint : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("point1", "The first point."),
            new CodeParameter("point2", "The second point.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            Element point1 = (Element)parameterValues[0];
            Element point2 = (Element)parameterValues[1];
            return (point1 + point2) / 2;
        }
    }

    [CustomMethod("LinearInterpolate", "Gets a point on a line with a fraction.", CustomMethodType.Value, typeof(VectorType))]
    class LinearInterpolate : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("point1", "The first point."),
                new CodeParameter("point2", "The second point."),
                new CodeParameter("fraction", "The fraction. 0 will return the first point, 1 will return the second point, 0.5 will return the midpoint, etc.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            Element point1 = (Element)parameters[0];
            Element point2 = (Element)parameters[1];
            Element fraction = (Element)parameters[2];

            Element p1 = point1 * (1 - fraction);
            Element p2 = point2 * fraction;

            return p1 + p2;
        }
    }

    // TODO: OptimisedLinearInterpolate
    /*
    [CustomMethod("OptimisedLinearInterpolate", CustomMethodType.MultiAction_Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    [Parameter("fraction", ValueType.Number, null)]
    class OptimisedLinearInterpolate : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar fraction = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "OptimisedLinearInterpolate: fraction", TranslateContext.IsGlobal);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                fraction.SetVariable((Element)Parameters[2])
            );

            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            Element p1 = point1 * (1 - fraction.GetVariable());
            Element p2 = point2 * fraction.GetVariable();

            return new MethodResult(actions, p1 + p2);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "A point a fraction along the distance between 2 points.",
                // Parameters
                "The first point.",
                "The second point.",
                "The fraction. 0 will return the first point, 1 will return the second point, 0.5 will return the midpoint, etc."
            );
        }
    }
    */

    [CustomMethod("LinearInterpolateDistance", "Gets a point on a line by distance.", CustomMethodType.Value, typeof(VectorType))]
    class LinearInterpolateDistance : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("point1", "The first point."),
                new CodeParameter("point2", "The second point."),
                new CodeParameter("distance", "The distance.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            Element point1 = (Element)parameters[0];
            Element point2 = (Element)parameters[1];
            Element distance = (Element)parameters[2];

            Element fraction = distance / Element.DistanceBetween(point1, point2);
            Element p1 = point1 * (1 - fraction);
            Element p2 = point2 * fraction;

            return p1 + p2;
        }
    }

    // TODO: OptimisedLinearInterpolateDistance
    /*
    [CustomMethod("OptimisedLinearInterpolateDistance", CustomMethodType.MultiAction_Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    [Parameter("distance", ValueType.Number, null)]
    class OptimisedLinearInterpolateDistance : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar fraction = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "OptimisedLinearInterpolateDistance: fraction", TranslateContext.IsGlobal);
            IndexedVar point1 = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "OptimisedLinearInterpolateDistance: point1", TranslateContext.IsGlobal);
            IndexedVar point2 = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "OptimisedLinearInterpolateDistance: point2", TranslateContext.IsGlobal);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                point1.SetVariable((Element)Parameters[0]),
                point2.SetVariable((Element)Parameters[1]),
                fraction.SetVariable((Element)Parameters[2] / Element.DistanceBetween(point1.GetVariable(), point2.GetVariable()))
            ) ;

            Element p1 = point1.GetVariable() * (1 - fraction.GetVariable());
            Element p2 = point2.GetVariable() * fraction.GetVariable();

            return new MethodResult(actions, p1 + p2);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "A point a distance along a straight line between 2 points.",
                // Parameters
                "The first point.",
                "The second point.",
                "The distance."
            );
        }
    }
    */
}
