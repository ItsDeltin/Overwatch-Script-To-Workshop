using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("Destination", CustomMethodType.Value)]
    [Parameter("startingPoint", ValueType.Vector, null)]
    [Parameter("direction", ValueType.Vector, null)]
    [Parameter("distance", ValueType.Number, null)]
    class Destination : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element startingPoint = (Element)Parameters[0];
            Element direction = (Element)Parameters[1];
            Element distance = (Element)Parameters[2];
            return new MethodResult(null, startingPoint + direction * distance);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Calculates a destination given a starting point, distance and direction",
                // Parameters
                "The starting point.",
                "The direction to move.",
                "The distance to move."
            );
        }
    }
}
