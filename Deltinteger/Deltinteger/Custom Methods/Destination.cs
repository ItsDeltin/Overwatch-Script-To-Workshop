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
            return new MethodResult(null, Element.Part<V_Add>(startingPoint, Element.Part<V_Multiply>(direction, distance)));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("Destination", "Calculates a destination given a starting point, distance and direction", null);
        }
    }
}
