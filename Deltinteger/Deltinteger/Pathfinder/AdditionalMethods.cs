using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    [CustomMethod("IsPathfinding", CustomMethodType.Value)]
    [Parameter("Player", Elements.ValueType.Player, null)]
    class IsPathfinding : CustomMethodBase
    {
        override protected MethodResult Get()
        {
            Element player = (Element)Parameters[0];

            if (TranslateContext.ParserData.PathfinderInfo == null)
                TranslateContext.ParserData.PathfinderInfo = new PathfinderInfo(TranslateContext.ParserData);
            PathfinderInfo pathfinderInfo = TranslateContext.ParserData.PathfinderInfo;

            Element isPathfinding = new V_Compare(
                Element.Part<V_CountOf>(pathfinderInfo.Path.GetVariable()),
                Operators.GreaterThan,
                new V_Number(0)
            );

            return new MethodResult(null, isPathfinding);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Checks if the target player is currently pathfinding with Pathfind().",
                "The player to check."
            );
        }
    }
}