using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathfinderInfo
    {
        public const double MoveToNext = 0.4;

        public IndexedVar Nodes { get; }
        public IndexedVar Path { get; }

        public PathfinderInfo(ParsingData parser)
        {
            Nodes = parser.VarCollection.AssignVar(null, "Pathfinder: Nodes", false, Variable.D, new int[0], null);
            Path = parser.VarCollection.AssignVar(null, "Pathfinder: Path", false, Variable.E, new int[0], null);

            parser.Rules.Add(GetStartRule());
            parser.Rules.Add(GetUpdateRule());
            parser.Rules.Add(GetStopRule());
        }

        private Rule GetStartRule()
        {
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
            return pathfind;
        }

        private Rule GetUpdateRule()
        {
            Element position = Element.Part<V_PositionOf>(new V_EventPlayer());
            Rule updateIndex = new Rule(Constants.INTERNAL_ELEMENT + "Pathfinder: Update", RuleEvent.OngoingPlayer);
            updateIndex.Conditions = new Condition[]
            {
                new Condition(
                    Element.Part<V_CountOf>(Path.GetVariable()),
                    Operators.GreaterThan,
                    new V_Number(0)
                ),
                // new Condition(
                //     Element.Part<V_DistanceBetween>(
                //         NextPosition(),
                //         position
                //     ),
                //     Operators.LessThan,
                //     new V_Number(MoveToNext)
                // )
                new Condition(
                    Element.Part<V_Or>(
                        new V_Compare(
                            Element.Part<V_DistanceBetween>(
                                NextPosition(),
                                position
                            ),
                            Operators.LessThan,
                            new V_Number(MoveToNext)
                        ),
                        Element.Part<V_And>(
                            new V_Compare(
                                Element.Part<V_CountOf>(Path.GetVariable()),
                                Operators.GreaterThan,
                                new V_Number(1)
                            ),
                            Element.Part<V_And>(
                                IsBetween(position, PositionAt(0), PositionAt(1)),
                                Element.Part<V_IsInLineOfSight>(position + new V_Vector(0, 1.5, 0), PositionAt(1) + new V_Vector(0, 1.5, 0))
                            )
                        )
                    )
                )
            };
            updateIndex.Actions = ArrayBuilder<Element>.Build(
                Path.SetVariable(Element.Part<V_ArraySlice>(Path.GetVariable(), new V_Number(1), new V_Number(Constants.MAX_ARRAY_LENGTH))),
                A_Wait.MinimumWait,
                new A_LoopIfConditionIsTrue()
            );
            return updateIndex;
        }

        private Rule GetStopRule()
        {
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
            return stop;
        }

        private Element NextPosition()
        {
            return Element.Part<V_ValueInArray>(Nodes.GetVariable(), Element.Part<V_FirstOf>(Path.GetVariable()));
        }

        private Element PositionAt(Element index)
        {
            return Element.Part<V_ValueInArray>(Nodes.GetVariable(), Element.Part<V_ValueInArray>(Path.GetVariable(), index));
        }

        private Element IsBetween(Element position, Element start, Element end)
        {
            return new V_Compare(
                Element.Part<V_DistanceBetween>((start + end) / 2, position),
                Operators.LessThanOrEqual,
                (Element.Part<V_DistanceBetween>(start, end) / 2)
            );
        }
    }
}