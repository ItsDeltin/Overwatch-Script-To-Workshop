using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("EyeCastHitPosition", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, null)]
    [Parameter("range", ValueType.Number, null)]
    class Test : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element player = (Element)Parameters[0];
            Element range = (Element)Parameters[1];
            Element eyePos = Element.Part<V_EyePosition>(player);
            Element direction = Element.Part<V_FacingDirectionOf>(player);
            Element raycast = Element.Part<V_RayCastHitPosition>(eyePos,
                Element.Part<V_Add>(
                    eyePos,
                    Element.Part<V_Multiply>(direction, range)
                    ),
                    new V_AllPlayers(),
                    new V_Null(),
                    new V_True()
                );
            return new MethodResult(null, raycast);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("EyeCastHitPosition", "Casts a ray in the direction the player is facing with a certain range.", null);
        }
    }
}