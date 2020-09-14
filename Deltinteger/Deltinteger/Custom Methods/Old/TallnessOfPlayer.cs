using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("TallnessOfPlayer", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    class TallnessOfPlayer : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element feetpos = Element.Part<V_PositionOf>((Element)Parameters[0]);
            Element eyepos = Element.Part<V_EyePosition>((Element)Parameters[0]);
            return new MethodResult(null, Element.YOf(eyepos) - Element.YOf(feetpos));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "The height of the player measured from feet to eyes. (totally useless)",
                "The player to get the height of."
            );
        }
    }
}