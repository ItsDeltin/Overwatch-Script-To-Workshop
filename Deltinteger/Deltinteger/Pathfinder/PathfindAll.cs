using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    [CustomMethod("PathfindAll", CustomMethodType.Action)]
    [Parameter("Players", Elements.ValueType.Player, null)]
    [VarRefParameter("Path Map")]
    [Parameter("Destination", Elements.ValueType.Vector, null)]
    class PathfindAll : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            if (((VarRef)Parameters[1]).Var is PathMapVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[1]).Var.Name, VarType.PathMap, ParameterLocations[1]);
            
            if (TranslateContext.ParserData.PathfinderInfo == null)
                TranslateContext.ParserData.PathfinderInfo = new PathfinderInfo(TranslateContext.ParserData);
            PathfinderInfo pathfinderInfo = TranslateContext.ParserData.PathfinderInfo;

            IndexedVar players = TranslateContext.VarCollection.AssignVar(Scope, "Players", TranslateContext.IsGlobal, Variable.O, new int[0], null);
            TranslateContext.Actions.AddRange(
                players.SetVariable((Element)Parameters[0])
            );

            PathMapVar pathmap = (PathMapVar)((VarRef)Parameters[1]).Var;

            IndexedVar destination = TranslateContext.VarCollection.AssignVar(Scope, "Destination", TranslateContext.IsGlobal, null);
            TranslateContext.Actions.AddRange(
                destination.SetVariable((Element)Parameters[2])
            );

            DijkstraMultiSource algorithm = new DijkstraMultiSource(TranslateContext, pathfinderInfo, pathmap, players.GetVariable(), destination.GetVariable());
            algorithm.Get();

            return new MethodResult(null, null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Moves an array of players to the specified position by pathfinding.",
                "The array of players to move.",
                "The path map.",
                "The destination to move the player to."
            );
        }
    }
}