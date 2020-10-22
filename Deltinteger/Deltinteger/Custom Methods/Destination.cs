using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("Destination", "Calculates a destination given a starting point, distance and direction", CustomMethodType.Value, typeof(VectorType))]
    class Destination : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("startingPoint", "The starting point."),
                new CodeParameter("direction", "The direction to move."),
                new CodeParameter("distance", "The distance to move.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            Element startingPoint = (Element)parameters[0];
            Element direction = (Element)parameters[1];
            Element distance = (Element)parameters[2];
            return startingPoint + direction * distance;
        }
    }
}
