using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Pathfinder
{
    // Pathmap object methods
    
    [CustomMethod("Pathfind", "Pathfinds a player.", CustomMethodType.Action, false)]
    class Pathfind : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("player", "The player to move."),
                new CodeParameter("destination", "the destination to move the player to.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            DijkstraNormal algorithm = new DijkstraNormal(
                actionSet, (Element)actionSet.CurrentObject.GetVariable(), Element.Part<V_PositionOf>(parameterValues[0]), (Element)parameterValues[1]
            );
            algorithm.Get();
            DijkstraBase.Pathfind(
                actionSet, actionSet.Translate.DeltinScript.SetupPathfinder(), (Element)algorithm.finalPath.GetVariable(), (Element)parameterValues[0], (Element)parameterValues[1]
            );

            return null;
        }
    }

    [CustomMethod("PathfindAll", "Moves an array of players to the specified position by pathfinding.", CustomMethodType.Action, false)]
    class PathfindAll : CustomMethodBase
    {
        public override CodeParameter[] Parameters()
        {
            return new CodeParameter[] {
                new CodeParameter("player", "The array of players to move."),
                new CodeParameter("destination", "The destination to move the players to.")
            };
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            DijkstraMultiSource algorithm = new DijkstraMultiSource(
                actionSet, actionSet.Translate.DeltinScript.SetupPathfinder(), (Element)actionSet.CurrentObject.GetVariable(), (Element)parameterValues[0], (Element)parameterValues[1]
            );
            algorithm.Get();

            return null;
        }
    }
}