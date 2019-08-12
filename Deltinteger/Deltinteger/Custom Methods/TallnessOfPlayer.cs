using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("TallnessOfPlayer", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, null)]
    class TallnessOfPlayer : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element feetpos = Element.Part<V_PositionOf>((Element)Parameters[0]);
            Element eyepos = Element.Part<V_EyePosition>((Element)Parameters[0]);
            return new MethodResult(null, Element.Part<V_Subtract>(Element.Part<V_YOf>(eyepos), Element.Part<V_YOf>(feetpos)));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("TallnessOfPlayer", "The height of the player measured from feet to eyes.", null);
        }
    }
}