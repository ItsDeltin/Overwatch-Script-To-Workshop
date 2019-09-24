using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathfinderInfo
    {
        public const double MoveToNext = 0.5;

        public IndexedVar Nodes { get; }
        public IndexedVar Path { get; }

        public PathfinderInfo(ParsingData parser)
        {
            Nodes = parser.VarCollection.AssignVar(null, "Pathfinder: Nodes", false, null);
            Path = parser.VarCollection.AssignVar(null, "Pathfinder: Path", false, null);

            Rule pathfind = new Rule(Constants.INTERNAL_ELEMENT + "Pathfinder: Move", RuleEvent.OngoingPlayer);
            pathfind.Conditions = new Condition[]
            {
                new Condition(
                    Element.Part<V_CountOf>(Path.GetVariable()),
                    Operators.GreaterThan,
                    new V_Number(0)
                )
            };
            pathfind.Actions = ArrayBuilder<Element>.Build
            (
                // Element.Part<A_StartFacing>(
                //     new V_EventPlayer(),
                //     Element.Part<V_DirectionTowards>(
                //         new V_EyePosition(),
                //         NextPosition()
                //     ),
                //     new V_Number(700),
                //     EnumData.GetEnumValue(Relative.ToWorld),
                //     EnumData.GetEnumValue(FacingRev.DirectionAndTurnRate)
                // ),
                Element.Part<A_StartThrottleInDirection>(
                    new V_EventPlayer(),
                    Element.Part<V_DirectionTowards>(
                        new V_EyePosition(),
                        NextPosition()
                    ),
                    new V_Number(1),
                    EnumData.GetEnumValue(Relative.ToWorld),
                    EnumData.GetEnumValue(ThrottleBehavior.ReplaceExistingThrottle),
                    EnumData.GetEnumValue(ThrottleRev.DirectionAndMagnitude)
                )
            );

            Rule updateIndex = new Rule(Constants.INTERNAL_ELEMENT + "Pathfinder: Update", RuleEvent.OngoingPlayer);
            updateIndex.Conditions = new Condition[]
            {
                new Condition(
                    Element.Part<V_CountOf>(Nodes.GetVariable()),
                    Operators.GreaterThan,
                    new V_Number(0)
                ),
                new Condition(
                    Element.Part<V_DistanceBetween>(
                        NextPosition(),
                        Element.Part<V_PositionOf>(new V_EventPlayer())
                    ),
                    Operators.LessThan,
                    new V_Number(MoveToNext)
                )
            };
            updateIndex.Actions = ArrayBuilder<Element>.Build(
                Path.SetVariable(Element.Part<V_ArraySlice>(Path.GetVariable(), new V_Number(1), new V_Number(Constants.MAX_ARRAY_LENGTH))),
                A_Wait.MinimumWait,
                new A_LoopIfConditionIsTrue()
            );

            Rule stop = new Rule(Constants.INTERNAL_ELEMENT + "Pathfinder: Stop", RuleEvent.OngoingPlayer);
            stop.Conditions = new Condition[]
            {
                new Condition(
                    Element.Part<V_CountOf>(Path.GetVariable()),
                    Operators.Equal,
                    new V_Number(0)
                )
            };
            stop.Actions = ArrayBuilder<Element>.Build(
                Element.Part<A_StopFacing>(new V_EventPlayer()),
                Element.Part<A_StopThrottleInDirection>(new V_EventPlayer())
            );

            parser.Rules.Add(pathfind);
            parser.Rules.Add(updateIndex);
            parser.Rules.Add(stop);
        }

        private Element NextPosition()
        {
            return Element.Part<V_ValueInArray>(Nodes.GetVariable(), Element.Part<V_FirstOf>(Path.GetVariable()));
        }
    }
}