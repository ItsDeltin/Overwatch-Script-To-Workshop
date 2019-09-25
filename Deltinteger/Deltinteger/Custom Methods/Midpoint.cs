using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("Midpoint", CustomMethodType.Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    class Midpoint : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            return new MethodResult(null, (point1 + point2) / 2);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The midpoint between 2 vectors.",
                // Parameters
                "The first point.",
                "The second point."
            );
        }
    }
}
