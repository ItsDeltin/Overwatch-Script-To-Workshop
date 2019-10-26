using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Pathfinder;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public abstract class PathfindPlayer : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            if (TranslateContext.ParserData.PathfinderInfo == null)
                TranslateContext.ParserData.PathfinderInfo = new PathfinderInfo(TranslateContext.ParserData);
            PathfinderInfo info = TranslateContext.ParserData.PathfinderInfo;
            return Get(info);
        }

        protected abstract MethodResult Get(PathfinderInfo info);
    }

    [CustomMethod("GetPath", CustomMethodType.MultiAction_Value)]
    [VarRefParameter("Path Map")]
    [Parameter("Position", Elements.ValueType.Vector, null)]
    [Parameter("Destination", Elements.ValueType.Vector, null)]
    public class GetPath : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            if (((VarRef)Parameters[0]).Var is PathMapVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[0]).Var.Name, VarType.PathMap, ParameterLocations[0]);
            
            PathMapVar pathmap = (PathMapVar)((VarRef)Parameters[0]).Var;
            Element position               = (Element)Parameters[1];
            Element destination            = (Element)Parameters[2];

            DijkstraNormal algorithm = new DijkstraNormal(TranslateContext, pathmap, position, destination);
            algorithm.Get();
            return new MethodResult(
                null,
                Element.Part<V_Append>(algorithm.finalPath.GetVariable(), destination)
            );
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Gets the path to the destination.",
                // Parameters
                "The path to use during pathfinding.",
                "The initial position.",
                "The final destination."
            );
        }
    }

    [CustomMethod("Pathfind", CustomMethodType.Action)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    [VarRefParameter("Path Map")]
    [Parameter("Destination", Elements.ValueType.Vector, null)]
    class Pathfind : PathfindPlayer
    {
        override protected MethodResult Get(PathfinderInfo info)
        {
            if (((VarRef)Parameters[1]).Var is PathMapVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[1]).Var.Name, VarType.PathMap, ParameterLocations[1]);
            
            Element player                 = (Element)Parameters[0];
            PathMapVar pathmap = (PathMapVar)((VarRef)Parameters[1]).Var;

            IndexedVar destination = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "Destination", TranslateContext.IsGlobal);
            TranslateContext.Actions.AddRange(destination.SetVariable((Element)Parameters[2]));

            DijkstraNormal algorithm = new DijkstraNormal(TranslateContext, pathmap, Element.Part<V_PositionOf>(player), destination.GetVariable());
            algorithm.Get();
            DijkstraBase.Pathfind(TranslateContext, info, algorithm.finalPath.GetVariable(), player, destination.GetVariable());
            return new MethodResult(null, null);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Moves a player to the specified position by pathfinding.",
                // Parameters
                "The player to move.",
                "The path to use during pathfinding.",
                "The destination to move the player to."
            );
        }
    }

    [CustomMethod("PathfindAll", CustomMethodType.Action)]
    [Parameter("Players", Elements.ValueType.Player, null)]
    [VarRefParameter("Path Map")]
    [Parameter("Destination", Elements.ValueType.Vector, null)]
    class PathfindAll : PathfindPlayer
    {
        protected override MethodResult Get(PathfinderInfo info)
        {
            if (((VarRef)Parameters[1]).Var is PathMapVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[1]).Var.Name, VarType.PathMap, ParameterLocations[1]);

            IndexedVar players = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "Players", TranslateContext.IsGlobal);
            TranslateContext.Actions.AddRange(players.SetVariable((Element)Parameters[0]));

            PathMapVar pathmap = (PathMapVar)((VarRef)Parameters[1]).Var;

            IndexedVar destination = IndexedVar.AssignInternalVarExt(TranslateContext.VarCollection, Scope, "Destination", TranslateContext.IsGlobal);
            TranslateContext.Actions.AddRange(destination.SetVariable((Element)Parameters[2]));

            DijkstraMultiSource algorithm = new DijkstraMultiSource(TranslateContext, info, pathmap, players.GetVariable(), destination.GetVariable());
            algorithm.Get();
            return new MethodResult(null, null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Moves an array of players to the specified position by pathfinding.",
                // Parameters
                "The array of players to move.",
                "The path to use during pathfinding.",
                "The destination to move the players to."
            );
        }
    }

    [CustomMethod("WalkPath", CustomMethodType.Action)]
    [Parameter("Players", Elements.ValueType.Player, null)]
    [Parameter("Path", Elements.ValueType.Any, null)]
    class WalkPath : PathfindPlayer
    {
        protected override MethodResult Get(PathfinderInfo info)
        {
            TranslateContext.Actions.AddRange(info.Path.SetVariable((Element)Parameters[1], (Element)Parameters[0]));
            return new MethodResult(null, null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Input players will walk to each position in the path.",
                "Players that will follow the path.",
                "The array of positions to walk to."
            );
        }
    }

    [CustomMethod("StopPathfind", CustomMethodType.Action)]
    [Parameter("Players", Elements.ValueType.Player, null)]
    class StopPathfind : PathfindPlayer
    {
        protected override MethodResult Get(PathfinderInfo info)
        {
            Element player = (Element)Parameters[0];

            return new MethodResult(ArrayBuilder<Element>.Build(
                info.Path.SetVariable(new V_EmptyArray(), player)
            ), null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Stops pathfinding for the specified players.",
                "The players that will stop pathfinding."
            );
        }
    }

    [CustomMethod("IsPathfinding", CustomMethodType.Value)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    class IsPathfinding : PathfindPlayer
    {
        override protected MethodResult Get(PathfinderInfo info)
        {
            Element player = (Element)Parameters[0];
            return new MethodResult(null, Get(info, player));
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Checks if the target player is currently pathfinding.",
                "The player to check."
            );
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

    [CustomMethod("IsPathfindUpdateSafe", CustomMethodType.Value)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    class IsPathfindUpdateSafe : PathfindPlayer
    {
        override protected MethodResult Get(PathfinderInfo info)
        {
            Element player = (Element)Parameters[0];

            Element isPathfinding = IsPathfinding.Get(info, player);

            Element isSafe = Element.Part<V_Or>(
                Element.Part<V_Not>(isPathfinding),
                Element.Part<V_IsTrueForAny>(
                    info.Path.GetVariable(player),
                    new V_Compare(
                        Element.Part<V_DistanceBetween>(Element.Part<V_PositionOf>(player), new V_ArrayElement()),
                        Operators.LessThanOrEqual,
                        PathfinderInfo.MoveToNext
                    )
                )
            );
            return new MethodResult(null, isSafe);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Checks if updating the pathfinding of a player is currently safe.",
                "The player to check."
            );
        }
    }

    [CustomMethod("IsPathfindStuck", CustomMethodType.Value)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    [Parameter("Speed Scalar", Elements.ValueType.Number, null)]
    class IsPathfindStuck : PathfindPlayer
    {
        override protected MethodResult Get(PathfinderInfo info)
        {
            Element leniency = 2;

            Element player = (Element)Parameters[0];
            Element scalar = (Element)Parameters[1];
            Element defaultSpeed = 5.5;
            Element nodeDistance = info.DistanceToNext.GetVariable(player);
            Element timeSinceLastNode = new V_TotalTimeElapsed() - info.LastUpdate.GetVariable(player);
            
            Element isStuck = new V_Compare(
                nodeDistance - ((defaultSpeed * scalar * timeSinceLastNode) / leniency),
                Operators.LessThanOrEqual,
                0
            );
            isStuck = Element.Part<V_And>(IsPathfinding.Get(info, player), isStuck);
            return new MethodResult(null, isStuck);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Returns true if the specified player takes longer than expected to reach the next pathfind node.",
                "The player to check.",
                "The speed scalar of the player."
            );
        }
    }

    [CustomMethod("FixPathfind", CustomMethodType.Action)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    class FixPathfind : PathfindPlayer
    {
        override protected MethodResult Get(PathfinderInfo info)
        {
            Element player = (Element)Parameters[0];
            TranslateContext.Actions.AddRange(ArrayBuilder<Element>.Build(
                Element.Part<A_Teleport>(player, info.NextPosition(player))
            ));
            return new MethodResult(null, null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Fixes pathfinding for a player by teleporting them to the next node. Use in conjunction with IsPathfindStuck().",
                "The player to fix pathfinding for."
            );
        }
    }

    [CustomMethod("NextNode", CustomMethodType.Value)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    class NextNode : PathfindPlayer
    {
        override protected MethodResult Get(PathfinderInfo info)
        {
            Element player = (Element)Parameters[0];
            return new MethodResult(null, info.NextPosition(player));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Gets the position of the next node.",
                "The player to get the next node of."
            );
        }
    }
}