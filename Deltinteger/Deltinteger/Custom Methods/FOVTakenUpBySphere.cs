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
            Element angle = Element.Part<V_ArcsineInDegrees>(Element.Part<V_Divide>(radius, distance));
            return new MethodResult(null, Element.Part<V_Multiply>(angle, new V_Number(2)));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("Midpoint", "The midpoint between 2 vectors.", null);
        }
    }
}
