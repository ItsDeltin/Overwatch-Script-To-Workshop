using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathfinderInfo
    {
        public const double MoveToNext = 0.3;

        public IndexedVar Nodes { get; }
        public IndexedVar Path { get; }
        public IndexedVar LastUpdate { get; }
        public IndexedVar DistanceToNext { get; }

        public PathfinderInfo(ParsingData parser)
        {
            Nodes = parser.VarCollection.AssignVar(null, "Pathfinder: Nodes", false, null);
            Path = parser.VarCollection.AssignVar(null, "Pathfinder: Path", false, null);
            LastUpdate = parser.VarCollection.AssignVar(null, "Pathfinder: Last Update", false, null);
            DistanceToNext = parser.VarCollection.AssignVar(null, "Pathfinder: Distance To Next Node", false, null);

            parser.Rules.Add(GetStartRule());
            parser.Rules.Add(GetUpdateRule());
            parser.Rules.Add(GetStopRule());
        }

        private Rule GetStartRule()
        {
            Rule pathfind = new Rule(Constants.INTERNAL_ELEMENT + "Pathfinder: Move", RuleEvent.OngoingPlayer);
            pathfind.Conditions = new Condition[]
            {
                IsPathfinding()
            };
            pathfind.Actions = ArrayBuilder<Element>.Build
            (
                LastUpdate.SetVariable(new V_TotalTimeElapsed()),
                DistanceToNext.SetVariable(Element.Part<V_DistanceBetween>(Element.Part<V_PositionOf>(new V_EventPlayer()), NextPosition(new V_EventPlayer()))),
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

                // Move to the next node.
                Element.Part<A_StartThrottleInDirection>(
                    new V_EventPlayer(),
                    Element.Part<V_DirectionTowards>(
                        new V_EyePosition(),
                        NextPosition(new V_EventPlayer()) // Because of ThrottleRev this will be reevaluated so 'Start Throttle In Direction' only needs to run once.
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
            // Once a node is reached during pathfinding, start traveling to the next node.

            //   If the distance between the player and the current node is less than 0.4 meters away (1)
            // OR
            //   the number of nodes is 2 or greater (2), the player is between the current node and the next node (3), and the player is in line of sight of the next node (4),
            // start traveling to the next node. (5)

            Element position = Element.Part<V_PositionOf>(new V_EventPlayer());
            Rule updateIndex = new Rule(Constants.INTERNAL_ELEMENT + "Pathfinder: Update", RuleEvent.OngoingPlayer);
            updateIndex.Conditions = new Condition[]
            {
                IsPathfinding(),
                new Condition(
                    Element.Part<V_Or>(
                        // (1)
                        new V_Compare(
                            Element.Part<V_DistanceBetween>(
                                NextPosition(new V_EventPlayer()),
                                position
                            ),
                            Operators.LessThan,
                            new V_Number(MoveToNext)
                        ),
                        Element.Part<V_And>(
                            // (2)
                            new V_Compare(
                                Element.Part<V_CountOf>(Path.GetVariable()),
                                Operators.GreaterThan,
                                new V_Number(1)
                            ),
                            Element.Part<V_And>(
                                // (3)
                                IsBetween(position, PositionAt(new V_EventPlayer(), 0), PositionAt(new V_EventPlayer(), 1)),
                                // (4)
                                Element.Part<V_IsInLineOfSight>(position + new V_Vector(0, 1.5, 0), PositionAt(new V_EventPlayer(), 1) + new V_Vector(0, 1.5, 0))
                            )
                        )
                    )
                )
            };
            updateIndex.Actions = ArrayBuilder<Element>.Build(
                LastUpdate.SetVariable(new V_TotalTimeElapsed()),
                Path.SetVariable(Element.Part<V_ArraySlice>(Path.GetVariable(), new V_Number(1), new V_Number(Constants.MAX_ARRAY_LENGTH))), // (5)
                DistanceToNext.SetVariable(Element.Part<V_DistanceBetween>(Element.Part<V_PositionOf>(new V_EventPlayer()), NextPosition(new V_EventPlayer()))),
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

        public Element NextPosition(Element player)
        {
            return Element.Part<V_ValueInArray>(Nodes.GetVariable(player), Element.Part<V_FirstOf>(Path.GetVariable(player)));
        }

        public Element PositionAt(Element player, Element index)
        {
            return Element.Part<V_ValueInArray>(Nodes.GetVariable(player), Element.Part<V_ValueInArray>(Path.GetVariable(player), index));
        }

        private Element IsBetween(Element position, Element start, Element end)
        {
            return new V_Compare(
                Element.Part<V_DistanceBetween>((start + end) / 2, position),
                Operators.LessThanOrEqual,
                (Element.Part<V_DistanceBetween>(start, end) / 2)
            );
        }

        private Condition IsPathfinding()
        {
            return new Condition(
                Element.Part<V_CountOf>(Path.GetVariable()),
                Operators.GreaterThan,
                0
            );
        }
    }
}