using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("SphereHitbox", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, null)]
    [Parameter("spherePosition", ValueType.VectorAndPlayer, null)]
    [Parameter("sphereRadius", ValueType.Number, null)]
    class SphereHitbox : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element player = (Element)Parameters[0];
            Element position = (Element)Parameters[1];
            Element radius = (Element)Parameters[2];
            Element eyePos = Element.Part<V_EyePosition>(player);
            Element range = Element.Part<V_DistanceBetween>(eyePos, position);
            Element direction = Element.Part<V_FacingDirectionOf>(player);
            Element raycast = Element.Part<V_RayCastHitPosition>(eyePos,
                Element.Part<V_Add>(
                    eyePos,
                    Element.Part<V_Multiply>(direction, range)
                    ),
                    new V_AllPlayers(),
                    new V_Null(),
                    new V_False()
                );
            Element distance = Element.Part<V_DistanceBetween>(position, raycast);
            Element compare = Element.Part<V_Compare>(distance, EnumData.GetEnumValue(Operators.LessThanOrEqual), radius);
            return new MethodResult(null, compare);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("SphereHitbox", "Whether the given player is looking directly at a sphere with collision.", null);
        }
    }
}
