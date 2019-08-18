using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("IsOnScreen", CustomMethodType.Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("point", ValueType.Vector, null)]
    [Parameter("fovOfPlayer", ValueType.Number, null)]
    class IsOnScreen : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element player = (Element)Parameters[0];
            Element point = (Element)Parameters[1];
            Element fov = (Element)Parameters[2];
            Element los = Element.Part<V_IsInLineOfSight>(Element.Part<V_EyePosition>(player), point);
            Element angle = Element.Part<V_IsInViewAngle>(player, point, Element.Part<V_Divide>(fov, new V_Number(2)));
            return new MethodResult(null, Element.Part<V_And>(los, angle));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Whether a point is visible on a players screen or not.",
                // Parameters
                "The target player.",
                "The point to check.",
                "The FOV of the player."
            );
        }
    }

    [CustomMethod("OptimisedIsOnScreen", CustomMethodType.MultiAction_Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("point", ValueType.Vector, null)]
    [Parameter("fovOfPlayer", ValueType.Number, null)]
    class OptimisedIsOnScreen : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedIsOnScreen: player", TranslateContext.IsGlobal, null);
            IndexedVar point = TranslateContext.VarCollection.AssignVar(Scope, "OptimisedIsOnScreen: point", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0]),
                point.SetVariable((Element)Parameters[1])
            );

            Element fov = (Element)Parameters[2];
            Element los = Element.Part<V_IsInLineOfSight>(Element.Part<V_EyePosition>(player.GetVariable()), point.GetVariable());
            Element angle = Element.Part<V_IsInViewAngle>(player.GetVariable(), point.GetVariable(), Element.Part<V_Divide>(fov, new V_Number(2)));

            return new MethodResult(actions, Element.Part<V_And>(los, angle));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Whether a point is visible on a players screen or not.",
                // Parameters
                "The target player.",
                "The point to check.",
                "The FOV of the player."
            );
        }
    }
}
