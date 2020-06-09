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
    [CustomMethod("IsPathfinding", "Checks if the target player is currently pathfinding.", CustomMethodType.Value, false)]
    class IsPathfinding : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("players", "The player to check.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return Get(actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>(), (Element)parameterValues[0]);
        }

        public static Element Get(PathfinderInfo info, Element player)
        {
            return new V_Compare(
                Element.Part<V_CountOf>(info.Path.GetVariable(player)),
                Operators.GreaterThan,
                new V_Number(0)
            );
        }
    }

    [CustomMethod("IsPathfindStuck", "Returns true if the specified player takes longer than expected to reach the next pathfind node.", CustomMethodType.Value, false)]
    class IsPathfindStuck : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("player", "The player to check."),
            new CodeParameter(
                "speedScalar",
                "The speed scalar of the player. `1` is the default speed of all heroes except Gengi and Tracer, which is `1.1`. Default value is `1`.",
                new ExpressionOrWorkshopValue(new V_Number(1))
            )
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            PathfinderInfo info = actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>();

            Element leniency = 2;

            Element player = (Element)parameterValues[0];
            Element scalar = (Element)parameterValues[1];
            Element defaultSpeed = 5.5;
            Element nodeDistance = (Element)info.DistanceToNext.GetVariable(player);
            Element timeSinceLastNode = new V_TotalTimeElapsed() - (Element)info.LastUpdate.GetVariable(player);
            
            Element isStuck = new V_Compare(
                nodeDistance - ((defaultSpeed * scalar * timeSinceLastNode) / leniency),
                Operators.LessThanOrEqual,
                new V_Number(0)
            );
            return Element.Part<V_And>(IsPathfinding.Get(info, player), isStuck);
        }
    }

    [CustomMethod("FixPathfind", "Fixes pathfinding for a player by teleporting them to the next node. Use in conjunction with `IsPathfindStuck()`.", CustomMethodType.Action, false)]
    class FixPathfind : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("player", "The player to fix pathfinding for."),
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            Element player = (Element)parameterValues[0];

            actionSet.AddAction(Element.Part<A_Teleport>(
                player,
                actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>().NextPosition(player)
            ));

            return null;
        }
    }

    [CustomMethod("NextNode", "Gets the position of the next node.", CustomMethodType.Value, false)]
    class NextNode : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("player", "The player to get the next node of."),
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            return actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>().NextPosition((Element)parameterValues[0]);
        }
    }

    [CustomMethod("WalkPath", "Input players will walk to each position in the path.", CustomMethodType.Action, false)]
    class WalkPath : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("players", "Players that will follow the path."),
            new CodeParameter("path", "The array of positions to walk to.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet.AddAction(actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>().Path.SetVariable(
                (Element)parameterValues[1],
                (Element)parameterValues[0]
            ));
            return null;
        }
    }

    [CustomMethod("SegmentAttribute", "Gets the attribute of a pathfind segment.", CustomMethodType.Value, false)]
    [Parameter("player", Elements.ValueType.Player, null)]
    [Parameter("segment", Elements.ValueType.Number, null)]
    class SegmentAttribute : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("player", "The player to get the segment attribute of."),
            new CodeParameter("segment", "The index of the segment.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            PathfinderInfo pathfindInfo = actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>();
            Element player  = (Element)parameterValues[0];
            Element segment = (Element)parameterValues[1];

            return ((Element)pathfindInfo.PathAttributes.GetVariable(player))[segment];
        }
    }

    [CustomMethod("RestartThrottle", "Throttle towards the next node.", CustomMethodType.Action, false)]
    class RestartThottle : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("player", "The player to restart throttle for.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            PathfinderInfo pathfindInfo = actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>();
            actionSet.AddAction(pathfindInfo.Throttle((Element)parameterValues[0]));
            return null;
        }
    }
}