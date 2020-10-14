using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("SetHealth", CustomMethodType.Action)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("health", ValueType.Number, null)]
    class SetHealth : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "SetHealth: player", TranslateContext.IsGlobal);
            IndexedVar health = IndexedVar.AssignInternalVar(TranslateContext.VarCollection, Scope, "SetHealth: health", TranslateContext.IsGlobal);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0]),
                health.SetVariable((Element)Parameters[1]),
                Element.Part<A_SkipIf>(!(Element.Part<V_Health>(player.GetVariable()) < health.GetVariable()), Element.Num(2)),
                Element.Part<A_Heal>(player.GetVariable(), new V_Null(), health.GetVariable() - Element.Part<V_Health>(player.GetVariable())),
                Element.Part<A_Skip>(Element.Num(2)),
                Element.Part<A_SkipIf>(!(Element.Part<V_Health>(player.GetVariable()) > health.GetVariable()), Element.Num(1)),
                Element.Part<A_Damage>(player.GetVariable(), new V_Null(), Element.Part<V_Health>(player.GetVariable()) - health.GetVariable())
            );

            return new MethodResult(actions, null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Sets the health of a player.",
                "The resulting health of the player."
            );
        }
    }
}