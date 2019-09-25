using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("FOVTakenUpBySphere", CustomMethodType.Value)]
    [Parameter("distanceToSphereCenter", ValueType.Number, null)]
    [Parameter("sphereRadius", ValueType.Number, null)]
    class FOVTakenUpBySphere : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element distance = (Element)Parameters[0];
            Element radius = (Element)Parameters[1];
            Element angle = Element.Part<V_ArcsineInDegrees>(radius / distance);
            return new MethodResult(null, angle * 2);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The angle of field of view a sphere will take up at a specific distance from an eye.",
                // Parameters
                "The distance to the center of the sphere from an eye.",
                "The radius of the sphere."
            );
        }
    }
}
