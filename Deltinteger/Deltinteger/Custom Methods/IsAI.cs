using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("IsAI", "Whether the player is an AI or not.", CustomMethodType.MultiAction_Value)]
    class IsAI : CustomMethodBase
    {
        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Element player = (Element)parameterValues[0];

            IndexReference originalHero = actionSet.VarCollection.Assign("isAI_originalHero", actionSet.IsGlobal, true);
            IndexReference isAI = actionSet.VarCollection.Assign("isAI_originalHero", actionSet.IsGlobal, true);

            actionSet.AddAction(ArrayBuilder<Element>.Build
            (
                originalHero.SetVariable(Element.Part<V_HeroOf>(player)),
                Element.Part<A_SkipIf>(Element.Part<V_Compare>(originalHero.GetVariable(), EnumData.GetEnumValue(Operators.NotEqual), new V_Null()), new V_Number(2)),
                isAI.SetVariable(new V_False()),
                Element.Part<A_Skip>(new V_Number(4)),
                Element.Part<A_ForcePlayerHero>(player, EnumData.GetEnumValue(Hero.Ashe)),
                isAI.SetVariable(Element.Part<V_Compare>(Element.Part<V_HeroOf>(player), EnumData.GetEnumValue(Operators.NotEqual), EnumData.GetEnumValue(Hero.Ashe))),
                Element.Part<A_ForcePlayerHero>(player, originalHero.GetVariable()),
                Element.Part<A_StopForcingHero>(player)
            ));
            
            return isAI.GetVariable();
        }

        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("player", "The player to check.")
            };
        }
    }
}