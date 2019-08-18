using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("SphereHitbox", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
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

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Whether the given player is looking directly at a sphere with collision.",
                // Parameters
                "The player.",
                "The position of the sphere.",
                "The radius of the sphere."
            );
        }
    }

    [CustomMethod("OptimisedSphereHitbox", CustomMethodType.MultiAction_Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("spherePosition", ValueType.VectorAndPlayer, null)]
    [Parameter("sphereRadius", ValueType.Number, null)]
    class OptimisedSphereHitbox : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedSphereHitbox: player", TranslateContext.IsGlobal, null);
            IndexedVar position = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedSphereHitbox: position", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0]),
                position.SetVariable((Element)Parameters[1])
            );

            Element radius = (Element)Parameters[2];
            Element eyePos = Element.Part<V_EyePosition>(player.GetVariable());
            Element range = Element.Part<V_DistanceBetween>(eyePos, position.GetVariable());
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
            Element distance = Element.Part<V_DistanceBetween>(position.GetVariable(), raycast);
            Element compare = Element.Part<V_Compare>(distance, EnumData.GetEnumValue(Operators.LessThanOrEqual), radius);

            return new MethodResult(actions, compare);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Whether the given player is looking directly at a sphere with collision.",
                // Parameters
                "The player.",
                "The position of the sphere.",
                "The radius of the sphere."
            );
        }
    }
}
