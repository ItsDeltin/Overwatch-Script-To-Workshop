using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("IsAIUnintrusive", CustomMethodType.Value)]
    [Parameter("player", ValueType.Number, typeof(V_EventPlayer))]
    class IsAIUnintrusive : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar player = TranslateContext.VarCollection.AssignVar(Scope, "IsAIUnintrusive: temp", TranslateContext.IsGlobal, null);

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
}