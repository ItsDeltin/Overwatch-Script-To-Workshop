using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("IsAIUnintrusive", CustomMethodType.MultiAction_Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    class IsAIUnintrusive : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "IsAIUnintrusive: player", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0]),
                Element.Part<A_Communicate>(player.GetVariable(), EnumData.GetEnumValue(Communication.VoiceLineUp))
            );

            Element result = Element.Part<V_Not>(Element.Part<V_IsCommunicating>(player.GetVariable(), EnumData.GetEnumValue(Communication.VoiceLineUp)));

            return new MethodResult(actions, result);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("IsAIUnintrusive", "Whether the player is an AI or not. Works in less situations but much less intrusive. Requires the player to be spawned in.", null);
        }
    }

    [CustomMethod("IsAIAccurate", CustomMethodType.MultiAction_Value)]
    [Parameter("player", ValueType.Player, typeof(V_EventPlayer))]
    class IsAIAccurate : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "IsAIAccurate: player", TranslateContext.IsGlobal, null);
            IndexedVar originalHero = TranslateContext.VarCollection.AssignVar(Scope, "IsAIAccurate: originalHero", TranslateContext.IsGlobal, null);
            IndexedVar isAI = TranslateContext.VarCollection.AssignVar(Scope, "IsAIAccurate: isAI", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
                player.SetVariable((Element)Parameters[0]),
                originalHero.SetVariable(Element.Part<V_HeroOf>(player.GetVariable())),
                Element.Part<A_SkipIf>(Element.Part<V_Not>(Element.Part<V_Compare>(originalHero.GetVariable(), EnumData.GetEnumValue(Operators.Equal), new V_Null())), new V_Number(2)),
                isAI.SetVariable(new V_False()),
                Element.Part<A_Skip>(new V_Number(4)),
                Element.Part<A_ForcePlayerHero>(player.GetVariable(), EnumData.GetEnumValue(Hero.Ashe)),
                isAI.SetVariable(Element.Part<V_Compare>(Element.Part<V_HeroOf>(player.GetVariable()), EnumData.GetEnumValue(Operators.NotEqual), EnumData.GetEnumValue(Hero.Ashe))),
                Element.Part<A_ForcePlayerHero>(player.GetVariable(), originalHero.GetVariable()),
                Element.Part<A_StopForcingHero>(player.GetVariable())
            );

            Element result = isAI.GetVariable();
            
            return new MethodResult(actions, result);
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("IsAIAccurate", "Whether the player is an AI or not. Works in more situations but it is more intrusive.", null);
        }
    }
}