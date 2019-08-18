using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("EyeCastHitPosition", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("range", ValueType.Number, null)]
    class EyeCastHitPosition : CustomMethodBase
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
                    new V_False()
                );
            return new MethodResult(null, raycast);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Casts a ray in the direction the player is facing with a certain range.",
                // Parameters
                "The player to perform the raycast.",
                "The range of the raycast."
            );
        }
    }

    [CustomMethod("OptimisedEyeCastHitPosition", CustomMethodType.MultiAction_Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("range", ValueType.Number, null)]
    class OptimisedEyeCastHitPosition : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedEyeCastHitPosition: player", TranslateContext.IsGlobal, null);

            Element range = (Element)Parameters[1];
            Element eyePos = Element.Part<V_EyePosition>(player.GetVariable());
            Element direction = Element.Part<V_FacingDirectionOf>(player.GetVariable());
            Element raycast = Element.Part<V_RayCastHitPosition>(eyePos,
                Element.Part<V_Add>(
                    eyePos,
                    Element.Part<V_Multiply>(direction, range)
                    ),
                    new V_AllPlayers(),
                    new V_Null(),
                    new V_False()
                );

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0])
            );

            return new MethodResult(actions, raycast);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Casts a ray in the direction the player is facing with a certain range.",
                // Parameters
                "The player to perform the raycast.",
                "The range of the raycast."
            );
        }
    }
}