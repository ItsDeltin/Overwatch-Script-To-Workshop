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
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "SetHealth: player", TranslateContext.IsGlobal, null);
            IndexedVar health = TranslateContext.VarCollection.AssignVar(Scope, "SetHealth: health", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0]),
                health.SetVariable((Element)Parameters[1]),
                Element.Part<A_SkipIf>(Element.Part<V_Not>(Element.Part<V_Compare>(Element.Part<V_Health>(player.GetVariable()), EnumData.GetEnumValue(Operators.LessThan), health.GetVariable())), new V_Number(2)),
                Element.Part<A_Heal>(player.GetVariable(), new V_Null(), Element.Part<V_Subtract>(health.GetVariable(), Element.Part<V_Health>(player.GetVariable()))),
                Element.Part<A_Skip>(new V_Number(2)),
                Element.Part<A_SkipIf>(Element.Part<V_Not>(Element.Part<V_Compare>(Element.Part<V_Health>(player.GetVariable()), EnumData.GetEnumValue(Operators.GreaterThan), health.GetVariable())), new V_Number(1)),
                Element.Part<A_Damage>(player.GetVariable(), new V_Null(), Element.Part<V_Subtract>(Element.Part<V_Health>(player.GetVariable()), health.GetVariable()))
            );

            return new MethodResult(actions, null);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("SetHealth", "Sets the health of a player.", null);
        }
    }
}