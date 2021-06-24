using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    public class ResolveInfoComponent : IComponent
    {
        public const double DefaultMoveToNext = 0.4;

        public DeltinScript DeltinScript { get; private set; }
        public bool TrackTimeSinceLastNode { get; set; } // This will be true if the Pathmap.IsPathfindingStuck function is called anywhere in the code.
        public bool PotentiallyNullNodes { get; set; } // Determines if nodes can potentially be null.
        public bool TrackNextAttribute { get; set; } // Determines if nodes can potentially be null.

        // Class Instances
        private PathmapClass PathmapInstance;

        // Workshop Variables
        private IndexReference DoGetCurrent { get; set; } // When set to true, the current node is reset to the closest node.
        public IndexReference Current { get; set; } // The index of the current node that the player is walking to.
        public IndexReference PathmapReference { get; set; } // A reference to the pathmap being used to pathfind.
        public IndexReference ParentArray { get; set; } // Stores the parent path array.
        private IndexReference Destination { get; set; } // The destination to walk to after all nodes have been transversed.
        public IndexReference CurrentAttribute { get; set; } // The current pathfinding attribute.
        public IndexReference NextAttribute { get; set; } // The current pathfinding attribute.

        // Stuck dection workshop variables. These are only assigned if 'TrackTimeSinceLastNode' is true.
        private IndexReference DistanceToNextNode { get; set; } // The distance from the player to the next node.
        private IndexReference TimeSinceLastNode { get; set; } // The time since the last node was reached.

        // Hooks to override ostw generated code.
        public LambdaAction OnPathStart { get; set; } // The code to run when a path starts.
        public LambdaAction OnNodeReached { get; set; } // The code to run when a node is reached.
        public LambdaAction OnPathCompleted { get; set; } // The code to run when a path is completed.
        public LambdaAction IsNodeReachedDeterminer { get; set; } // The condition that determines whether or not the current node was reached.
        public LambdaAction ApplicableNodeDeterminer { get; set; } // The function used to get the closest node to the player.

        ToWorkshop _toWorkshop;

        public void Init(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;
            _toWorkshop = deltinScript.WorkshopConverter;

            bool assignExtended = false;

            // Assign workshop variables.
            DoGetCurrent = DeltinScript.VarCollection.Assign("pathfinderDoGetCurrent", false, assignExtended);
            Current = DeltinScript.VarCollection.Assign("pathfinderCurrent", false, assignExtended);
            PathmapReference = DeltinScript.VarCollection.Assign("pathmapReference", false, assignExtended);
            ParentArray = DeltinScript.VarCollection.Assign("parentArray", false, assignExtended);
            Destination = DeltinScript.VarCollection.Assign("destination", false, assignExtended);
            CurrentAttribute = DeltinScript.VarCollection.Assign("lastAttribute", false, assignExtended);

            if (TrackTimeSinceLastNode)
            {
                DistanceToNextNode = DeltinScript.VarCollection.Assign("distanceToNextNode", false, assignExtended);
                TimeSinceLastNode = DeltinScript.VarCollection.Assign("timeSinceLastNode", false, assignExtended);
            }

            var pathfinderTypes = DeltinScript.GetComponent<PathfinderTypesComponent>();
            if (TrackNextAttribute)
                NextAttribute = DeltinScript.VarCollection.Assign("nextAttribute", false, assignExtended);

            // Get the PathResolve instance and the Pathmap instance.
            PathmapInstance = pathfinderTypes.Pathmap;

            // Get the resolve subroutine.
            GetResolveCurrentRule();
            GetNextNodeRule();
        }

        void GetResolveCurrentRule()
        {
            // Create the rule that will get the closest node.
            TranslateRule getResolveRule = new TranslateRule(DeltinScript, "Pathfinder: Resolve Current", RuleEvent.OngoingPlayer);
            // The rule will activate when DoGetCurrent is set to true.
            getResolveRule.Conditions.Add(new Condition((Element)DoGetCurrent.GetVariable(), Operator.Equal, Element.True()));
            // Set the Current variable to the closest node.
            getResolveRule.ActionSet.AddAction(Current.SetVariable(ClosestNode(getResolveRule.ActionSet, PlayerPosition())));

            // If the OnPathStart hook is null, do the default which is throttling the player to the next node.
            if (OnPathStart == null)
                // Start throttle to the current node.
                ThrottleEventPlayerToNextNode(getResolveRule.ActionSet);
            // Otherwise, use the hook.
            else
                OnPathStart.Invoke(getResolveRule.ActionSet);

            // Update IsPathfindStuck data.
            UpdateStuckDetector(getResolveRule.ActionSet, EventPlayer());

            // Reset DoGetCurrent to false.
            getResolveRule.ActionSet.AddAction(DoGetCurrent.SetVariable(Element.False()));

            // Add the rule.
            DeltinScript.WorkshopRules.Add(getResolveRule.GetRule());
        }

        void GetNextNodeRule()
        {
            // The 'next' rule will set current to the next node index when the current node is reached. 
            TranslateRule next = new TranslateRule(DeltinScript, "Pathfinder: Resolve Next", RuleEvent.OngoingPlayer);
            next.Conditions.Add(NodeReachedCondition(next.ActionSet));
            next.Conditions.Add(new Condition(ParentArray.Get(), Operator.NotEqual, Element.Null()));

            GetNextNode(next.ActionSet, EventPlayer());

            // Loop the next rule if the condition is true.
            next.ActionSet.AddAction(LoopIfConditionIsTrue());

            // Add rule
            DeltinScript.WorkshopRules.Add(next.GetRule());
        }

        public void GetNextNode(ActionSet actionSet, Element player)
        {
            if (OnPathCompleted == null || !OnPathCompleted.EmptyBlock)
                actionSet.AddAction(If(Compare(Current.Get(player), Operator.NotEqual, Num(-1))));

            // Get last attribute.
            actionSet.AddAction(CurrentAttribute.SetVariable(GetCurrentSegmentAttribute(player), targetPlayer: player));

            if (TrackNextAttribute)
                actionSet.AddAction(NextAttribute.SetVariable(GetNextSegmentAttribute(player), targetPlayer: player));

            // Set current as the current's parent.
            actionSet.AddAction(Current.SetVariable(ParentArray.Get(player)[Current.Get(player)] - 1, targetPlayer: player));

            // Update stuck
            UpdateStuckDetector(actionSet, player);

            // Invoke OnNodeReached
            OnNodeReached?.Invoke(actionSet);

            if (OnPathCompleted == null || !OnPathCompleted.EmptyBlock) actionSet.AddAction(Else());

            if (OnPathCompleted == null)
            {
                actionSet.AddAction(Part("Stop Throttle In Direction", player));
                StopPathfinding(actionSet, player);
            }
            else if (!OnPathCompleted.EmptyBlock) OnPathCompleted.Invoke(actionSet);

            if (OnPathCompleted == null || !OnPathCompleted.EmptyBlock) actionSet.AddAction(End());
        }

        /// <summary>Gets the closest node the player is pathfinding with.</summary>
        public void SetCurrent(ActionSet actionSet, Element players)
        {
            actionSet.AddAction(DoGetCurrent.SetVariable(value: Element.True(), targetPlayer: players));
        }

        /// <summary>Gets the closest node from a position.</summary>
        public Element ClosestNode(ActionSet actionSet, Element position) => PathmapInstance.GetNodeFromPositionHandler(actionSet, PathmapReference.Get()).NodeFromPosition(position);

        public Element CurrentPositionWithDestination(Element player = null) => PositionAtOrDestination(Current.Get(player), player);
        public Element NextPositionWithDestination(Element player = null) => PositionAtOrDestination(ParentArray.Get(player)[Current.GetVariable(player)] - 1, player);

        Element PositionAtOrDestination(Element node, Element player = null) => Element.TernaryConditional(
            // Current will be -1 if the player reached the last node.
            Element.Compare(node, Operator.Equal, Element.Num(-1)),
            // If so, go to the destination.
            Destination.GetVariable(player),
            // Otherwise, go to the current node.
            PathmapInstance.Nodes.Get(_toWorkshop, PathmapReference.Get(player))[node]
        );

        /// <summary>The position of the current player: `Position Of(Event Player)`</summary>
        private Element PlayerPosition() => Element.PositionOf(Element.EventPlayer());

        /// <summary>Starts pathfinding for the specified players.</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="players">The players that will start pathfinding.</param>
        /// <param name="pathmapReference">A reference to the pathmap the players are pathfinding with.</param>
        /// <param name="parentArray">The parent array path.</param>
        /// <param name="attributeArray">The path attributes.</param>
        /// <param name="destination">The destination the players are navigating to.</param>
        public void Pathfind(ActionSet actionSet, Element players, Element pathmapReference, Element parentArray, Element destination)
        {
            // Set target's pathmap reference.
            actionSet.AddAction(PathmapReference.SetVariable(
                value: pathmapReference,
                targetPlayer: players
            ));

            // Set target's parent array.
            actionSet.AddAction(ParentArray.SetVariable(
                value: parentArray,
                targetPlayer: players
            ));

            // Set target's destination.
            actionSet.AddAction(Destination.SetVariable(
                value: destination,
                targetPlayer: players
            ));

            // For each of the players, get the current.
            SetCurrent(actionSet, players);
        }

        /// <summary>Stops pathfinding for the specified players.</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="players">The players to stop pathfinding for.</param>
        public void StopPathfinding(ActionSet actionSet, Element players) => actionSet.AddAction(ParentArray.SetVariable(value: Element.Null(), targetPlayer: players));

        /// <summary>Stops pathfinding for the players that are pathfinding using the specified pathmap reference.</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="pathmapReference">The reference of the pathmap. Any players using this pathmap will stop pathfinding.</param>
        /// <param name="players">The players to stop pathfinding for.</param>
        public void StopPathfindingWithPathmap(ActionSet actionSet, Element pathmapReference, Element players) =>
            StopPathfinding(
                actionSet,
                // Filter players by whos pathfinding reference is equal to pathmapReference.
                Element.Filter(
                    players,
                    Element.Compare(
                        PathmapReference.GetVariable(Element.ArrayElement()),
                        Operator.Equal,
                        pathmapReference
                    )
                )
            );

        /// <summary>Determines if the target player is pathfinding.</summary>
        public Element IsPathfinding(Element player) => Element.Compare(ParentArray.GetVariable(player), Operator.NotEqual, Element.Null());

        /// <summary>Returns true if the player takes longer than expected to reach the next node.</summary>
        public Element IsPathfindingStuck(Element player, Element scalar)
        {
            Element leniency = 2;

            Element defaultSpeed = 5.5;
            Element nodeDistance = DistanceToNextNode.Get(player);
            Element timeSinceLastNode = Element.Part("Total Time Elapsed") - TimeSinceLastNode.Get(player);
            
            Element isStuck = Element.Compare(
                nodeDistance - ((defaultSpeed * scalar * timeSinceLastNode) / leniency),
                Operator.LessThanOrEqual,
                Element.Num(0)
            );
            return Element.And(IsPathfinding(player), isStuck);
        }

        /// <summary>Updates stuck detector.</summary>
        private void UpdateStuckDetector(ActionSet actionSet, Element player)
        {
            if (!TrackTimeSinceLastNode) return; // Do nothing if TrackTimeSinceLastNode is set to false.
            actionSet.AddAction(TimeSinceLastNode.SetVariable(Part("Total Time Elapsed"), targetPlayer: player));
            actionSet.AddAction(DistanceToNextNode.SetVariable(DistanceBetween(PositionOf(player), CurrentPositionWithDestination(player)), targetPlayer: player));
        }

        /// <summary>Gets the next pathfinding attribute.</summary>
        Element GetCurrentSegmentAttribute(Element player) => Element.Map(Element.Filter(
            PathmapInstance.Attributes.Get(_toWorkshop, PathmapReference.Get(player)),
            Element.And(
                Element.Compare(Element.XOf(Element.ArrayElement()), Operator.Equal, Current.Get(player)),
                Element.Compare(Element.YOf(Element.ArrayElement()), Operator.Equal, ParentArray.Get(player)[Current.Get(player)] - 1)
            )
        ), Element.ZOf(Element.ArrayElement()));

        Element GetNextSegmentAttribute(Element player) => Element.Map(Element.Filter(
            PathmapInstance.Attributes.Get(_toWorkshop, PathmapReference.Get(player)),
            Element.And(
                Element.Compare(Element.XOf(Element.ArrayElement()), Operator.Equal, ParentArray.Get(player)[Current.Get(player)] - 1),
                Element.Compare(Element.YOf(Element.ArrayElement()), Operator.Equal, ParentArray.Get(player)[ParentArray.Get(player)[Current.Get(player)] - 1] - 1)
            )
        ), Element.ZOf(Element.ArrayElement()));
    
        /// <summary>Throttles the event player to the next node.</summary>
        public void ThrottleEventPlayerToNextNode(ActionSet actionSet)
        {
            // Start throttle to the current node.
            actionSet.AddAction(Element.Part("Start Throttle In Direction",
                Element.EventPlayer(),
                Element.DirectionTowards(
                    Element.PositionOf(Element.EventPlayer()),
                    // Go to the destination once the final node is reached.
                    CurrentPositionWithDestination()
                ),
                Element.Num(1), // Magnitude
                ElementRoot.Instance.GetEnumValueFromWorkshop("Relative", "To World"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("ThrottleBehavior", "Replace Existing Throttle"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("ThrottleRev", "Direction And Magnitude")
            ));
        }

        /// <summary>The condition to use to determine when the current node is reached.</summary>
        private Condition NodeReachedCondition(ActionSet actionSet)
        {
            // No node reached hook, use default.
            if (IsNodeReachedDeterminer == null)
                return new Condition(
                    Element.DistanceBetween(
                        PlayerPosition(),
                        CurrentPositionWithDestination()
                    ),
                    Operator.LessThanOrEqual,
                    Element.Num(DefaultMoveToNext)
                );
            // Otherwise, use hook.
            else
                return new Condition(
                    (Element)IsNodeReachedDeterminer.Invoke(actionSet, CurrentPositionWithDestination()),
                    Operator.Equal,
                    Element.True()
                );
        }
    }
}