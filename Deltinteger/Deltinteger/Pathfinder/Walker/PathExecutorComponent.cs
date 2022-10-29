using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;


namespace Deltin.Deltinteger.Pathfinder.Walker
{
    class PathExecutorComponent : IComponent
    {
        public const double DefaultMoveToNext = 0.4;

        /// <summary>This will be true if the Pathmap.IsPathfindingStuck function is called anywhere in the code.</summary>
        public bool TrackTimeSinceLastNode { get; private set; }
        /// <summary>Determines if nodes can potentially be null.</summary>
        public bool PotentiallyNullNodes { get; private set; }
        /// <summary>If true, the next attribute in the path is saved.</summary>
        public bool TrackNextAttribute { get; private set; }

        // Workshop Variables
        /// <summary>The id of the currently running pathmap executor. **Set at start of pathfind.**</summary>
        public IndexReference CurrentPathmapExecutorID { get; private set; }
        /// <summary>When set to true, the current node is reset to the closest node. **Set at start of pathfind.**</summary>
        public IndexReference DoGetCurrent { get; private set; }
        /// <summary>The index of the current node that the player is walking to. **Set at start of pathfind.**</summary>
        public IndexReference Current { get; private set; }
        /// <summary>Stores the parent path array. Set at start of pathfind. **Set at start of pathfind.**</summary>
        public IndexReference ParentArray { get; private set; }
        /// <summary>The destination to walk to after all nodes have been transversed. **Set at start of pathfind.**</summary>
        public IndexReference Destination { get; private set; }
        /// <summary>The current pathfinding attribute.</summary>
        public IndexReference CurrentAttribute { get; private set; }
        /// <summary>The current pathfinding attribute.</summary>
        public IndexReference NextAttribute { get; private set; }

        // Stuck dection workshop variables. These are only assigned if 'TrackTimeSinceLastNode' is true.
        /// <summary>The distance from the player to the next node.</summary>
        public IndexReference DistanceToNextNode { get; private set; }
        /// <summary>The time since the last node was reached.</summary>
        public IndexReference TimeSinceLastNode { get; private set; }

        // Hooks to override ostw generated code.
        public LambdaAction OnPathStart { get; private set; } // The code to run when a path starts.
        public LambdaAction OnNodeReached { get; private set; } // The code to run when a node is reached.
        public LambdaAction OnPathCompleted { get; private set; } // The code to run when a path is completed.
        public LambdaAction IsNodeReachedDeterminer { get; private set; } // The condition that determines whether or not the current node was reached.
        public LambdaAction ApplicableNodeDeterminer { get; private set; } // The function used to get the closest node to the player.

        DeltinScript deltinScript;

        int ruleId = 0;

        ClassReferenceRule classExecutor = null;
        readonly List<StructPathExecutor> structExecutors = new List<StructPathExecutor>();

        public void Init(DeltinScript deltinScript)
        {
            this.deltinScript = deltinScript;

            bool assignExtended = false;

            // Assign workshop variables.
            CurrentPathmapExecutorID = deltinScript.VarCollection.Assign("currentPathmapExecutorID", false, assignExtended);
            DoGetCurrent = deltinScript.VarCollection.Assign("pathfinderDoGetCurrent", false, assignExtended);
            Current = deltinScript.VarCollection.Assign("pathfinderCurrent", false, assignExtended);
            ParentArray = deltinScript.VarCollection.Assign("parentArray", false, assignExtended);
            Destination = deltinScript.VarCollection.Assign("destination", false, assignExtended);
            CurrentAttribute = deltinScript.VarCollection.Assign("lastAttribute", false, assignExtended);

            if (TrackTimeSinceLastNode)
            {
                DistanceToNextNode = deltinScript.VarCollection.Assign("distanceToNextNode", false, assignExtended);
                TimeSinceLastNode = deltinScript.VarCollection.Assign("timeSinceLastNode", false, assignExtended);
            }

            var pathfinderTypes = deltinScript.GetComponent<PathfinderTypesComponent>();
            if (TrackNextAttribute)
                NextAttribute = deltinScript.VarCollection.Assign("nextAttribute", false, assignExtended);
        }

        public IPathExecutor ClassExcecutor(IWorkshopTree classReference)
        {
            if (classExecutor == null)
            {
                classExecutor = new ClassReferenceRule(AssignRule());
            }
            return classExecutor.WithReference((Element)classReference);
        }

        public IPathExecutor StructExecutor(IWorkshopTree structSource)
        {
            foreach (var executor in structExecutors)
                if (executor.Source.EqualTo(structSource))
                    return executor;

            var newExecutor = new StructPathExecutor(AssignRule(), structSource);
            this.structExecutors.Add(newExecutor);
            return newExecutor;
        }

        private ExecutorInstanceSetup AssignRule() => new ExecutorInstanceSetup(this, ++ruleId, deltinScript);
    }

    class ExecutorInstanceSetup
    {
        public PathExecutorComponent PathExecutorComponent => pec;
        public DeltinScript DeltinScript { get; }

        readonly PathExecutorComponent pec;
        readonly int id;
        IPathRule ruleHandler;

        public ExecutorInstanceSetup(PathExecutorComponent pec, int id, DeltinScript deltinScript)
        {
            this.pec = pec;
            this.id = id;
            DeltinScript = deltinScript;
        }

        public void Setup(IPathRule ruleHandler)
        {
            this.ruleHandler = ruleHandler;
            MakeResolveCurrentRule();
            MakeNextNodeRule();
        }

        void MakeResolveCurrentRule()
        {
            // Create the rule that will get the closest node.
            TranslateRule getResolveRule = new TranslateRule(DeltinScript, "Pathfinder: Resolve Current (" + id + ")", RuleEvent.OngoingPlayer);
            // The rule will activate when DoGetCurrent is set to true.
            getResolveRule.Conditions.Add(new Condition(pec.DoGetCurrent.Get(), Operator.Equal, True()));
            getResolveRule.Conditions.Add(new Condition(pec.CurrentPathmapExecutorID.Get(), Operator.Equal, Num(id)));
            // Set the Current variable to the closest node.
            getResolveRule.ActionSet.AddAction(pec.Current.SetVariable(ClosestNode(getResolveRule.ActionSet, PlayerPosition())));

            // If the OnPathStart hook is null, do the default which is throttling the player to the next node.
            if (pec.OnPathStart == null)
                // Start throttle to the current node.
                ThrottleEventPlayerToNextNode(getResolveRule.ActionSet);
            // Otherwise, use the hook.
            else
                pec.OnPathStart.Invoke(getResolveRule.ActionSet);

            // Update IsPathfindStuck data.
            UpdateStuckDetector(getResolveRule.ActionSet, EventPlayer());

            // Reset DoGetCurrent to false.
            getResolveRule.ActionSet.AddAction(pec.DoGetCurrent.SetVariable(Element.False()));

            // Add the rule.
            DeltinScript.WorkshopRules.Add(getResolveRule.GetRule());
        }

        void MakeNextNodeRule()
        {
            // The 'next' rule will set current to the next node index when the current node is reached. 
            TranslateRule next = new TranslateRule(DeltinScript, "Pathfinder: Resolve Next (" + id + ")", RuleEvent.OngoingPlayer);
            next.Conditions.Add(new Condition(pec.CurrentPathmapExecutorID.Get(), Operator.Equal, Num(id)));
            next.Conditions.Add(NodeReachedCondition(next.ActionSet));
            // TODO: parent array check is wierd
            // next.Conditions.Add(new Condition(pec.ParentArray.Get(), Operator.NotEqual, Element.Null()));

            SetNextNode(next.ActionSet, EventPlayer());

            // Loop the next rule if the condition is true.
            next.ActionSet.AddAction(LoopIfConditionIsTrue());

            // Add rule
            DeltinScript.WorkshopRules.Add(next.GetRule());
        }

        /// <summary>Gets the closest node from a position.</summary>
        Element ClosestNode(ActionSet actionSet, Element position, Element player = null)
        {
            Element nodes = ruleHandler.GetNodeArray(player ?? EventPlayer()), sortArray = nodes;

            // If nodes can be null, filter out the null nodes.
            if (ruleHandler.NodesMayBeNull())
                sortArray = Filter(nodes, Compare(ArrayElement(), Operator.NotEqual, Null()));

            if (pec.ApplicableNodeDeterminer == null)
            {
                // Return index of closest node to position.
                return IndexOfArrayValue(
                    nodes,
                    FirstOf(Sort(
                        sortArray,
                        DistanceBetween(position, ArrayElement())
                    ))
                );
            }
            else
            {
                return (Element)pec.ApplicableNodeDeterminer.Invoke(actionSet, sortArray, position);
            }
        }

        /// <summary>Gets position that the player is walking to.</summary>
        public Element CurrentPositionWithDestination(Element player = null) => PositionAtOrDestination(pec.Current.Get(player), player);

        /// <summary>Gets the position of the given node. If the node is -1, returns the final destination instead.</summary>
        Element PositionAtOrDestination(Element node, Element player = null) => Element.TernaryConditional(
            // Current will be -1 if the player reached the last node.
            Element.Compare(node, Operator.Equal, Num(-1)),
            // If so, go to the destination.
            pec.Destination.GetVariable(player),
            // Otherwise, go to the current node.
            ruleHandler.GetNodeArray(player)[node]
        );

        /// <summary>Gets the current pathfinding attribute.</summary>
        Element GetCurrentSegmentAttribute(Element player) => Map(Filter(
            ruleHandler.GetAttributesArray(player),
            And(
                Compare(XOf(ArrayElement()), Operator.Equal, pec.Current.Get(player)),
                Compare(YOf(ArrayElement()), Operator.Equal, pec.ParentArray.Get(player)[pec.Current.Get(player)] - 1)
            )
        ), ZOf(ArrayElement()));

        /// <summary>Gets the next pathfinding attribute.</summary>
        Element GetNextSegmentAttribute(Element player) => Map(Filter(
            ruleHandler.GetAttributesArray(player),
            And(
                Compare(XOf(ArrayElement()), Operator.Equal, pec.ParentArray.Get(player)[pec.Current.Get(player)] - 1),
                Compare(YOf(ArrayElement()), Operator.Equal, pec.ParentArray.Get(player)[pec.ParentArray.Get(player)[pec.Current.Get(player)] - 1] - 1)
            )
        ), ZOf(ArrayElement()));

        /// <summary>The position of the current player: `Position Of(Event Player)`</summary>
        Element PlayerPosition() => PositionOf(EventPlayer());

        /// <summary>The condition to use to determine when the current node is reached.</summary>
        Condition NodeReachedCondition(ActionSet actionSet)
        {
            // No node reached hook, use default.
            if (pec.IsNodeReachedDeterminer == null)
                return new Condition(
                    DistanceBetween(
                        PlayerPosition(),
                        CurrentPositionWithDestination()
                    ),
                    Operator.LessThanOrEqual,
                    Num(PathExecutorComponent.DefaultMoveToNext)
                );
            // Otherwise, use hook.
            else
                return new Condition(
                    (Element)pec.IsNodeReachedDeterminer.Invoke(actionSet, CurrentPositionWithDestination()),
                    Operator.Equal,
                    True()
                );
        }

        /// <summary>Sets the player's current node to the next node in the parent array. Should be called when a player reaches the node
        /// that they are walking towards.</summary>
        public void SetNextNode(ActionSet actionSet, Element player)
        {
            if (pec.OnPathCompleted == null || !pec.OnPathCompleted.EmptyBlock)
                actionSet.AddAction(If(Compare(pec.Current.Get(player), Operator.NotEqual, Num(-1))));

            // Get last attribute.
            actionSet.AddAction(pec.CurrentAttribute.SetVariable(GetCurrentSegmentAttribute(player), targetPlayer: player));

            if (pec.TrackNextAttribute)
                actionSet.AddAction(pec.NextAttribute.SetVariable(GetNextSegmentAttribute(player), targetPlayer: player));

            // Set current as the current's parent.
            actionSet.AddAction(pec.Current.SetVariable(pec.ParentArray.Get(player)[pec.Current.Get(player)] - 1, targetPlayer: player));

            // Update stuck
            UpdateStuckDetector(actionSet, player);

            // Invoke OnNodeReached
            pec.OnNodeReached?.Invoke(actionSet);

            if (pec.OnPathCompleted == null || !pec.OnPathCompleted.EmptyBlock) actionSet.AddAction(Else());

            if (pec.OnPathCompleted == null)
            {
                actionSet.AddAction(Part("Stop Throttle In Direction", player));
                StopPathfinding(actionSet, player);
            }
            else if (!pec.OnPathCompleted.EmptyBlock) pec.OnPathCompleted.Invoke(actionSet);

            if (pec.OnPathCompleted == null || !pec.OnPathCompleted.EmptyBlock) actionSet.AddAction(End());
        }

        /// <summary>Throttles the event player to the next node.</summary>
        public void ThrottleEventPlayerToNextNode(ActionSet actionSet)
        {
            // Start throttle to the current node.
            actionSet.AddAction(Part("Start Throttle In Direction",
                EventPlayer(),
                DirectionTowards(
                    PlayerPosition(),
                    // Go to the destination once the final node is reached.
                    CurrentPositionWithDestination()
                ),
                Num(1), // Magnitude
                ElementRoot.Instance.GetEnumValueFromWorkshop("Relative", "To World"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("ThrottleBehavior", "Replace Existing Throttle"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("ThrottleRev", "Direction And Magnitude")
            ));
        }

        /// <summary>Updates the closest node the player is pathfinding with.</summary>
        public void ActivateGetCurrentNodeRule(ActionSet actionSet, Element players) =>
            actionSet.AddAction(pec.DoGetCurrent.SetVariable(value: Element.True(), targetPlayer: players));

        public void Pathfind(ActionSet actionSet, Element players, Element parentArray, Element destination)
        {
            // Set the executor ID.
            pec.CurrentPathmapExecutorID.Set(actionSet, Num(id), players);

            // Set target's parent array.
            pec.ParentArray.Set(actionSet, parentArray, players);

            // Set target's destination.
            pec.Destination.Set(actionSet, destination, players);

            // Activate the rule that gets the current node.
            ActivateGetCurrentNodeRule(actionSet, players);
        }

        /// <summary>Stops pathfinding for the specified players.</summary>
        /// <param name="actionSet">The actionset of the current rule.</param>
        /// <param name="players">The players to stop pathfinding for.</param>
        public void StopPathfinding(ActionSet actionSet, Element players) => actionSet.AddAction(pec.ParentArray.SetVariable(value: Null(), targetPlayer: players));

        /// <summary>Updates stuck detector.</summary>
        void UpdateStuckDetector(ActionSet actionSet, Element player)
        {
            if (!pec.TrackTimeSinceLastNode) return; // Do nothing if TrackTimeSinceLastNode is set to false.
            pec.TimeSinceLastNode.Set(actionSet, Part("Total Time Elapsed"), player);
            pec.DistanceToNextNode.Set(actionSet, DistanceBetween(PositionOf(player), CurrentPositionWithDestination(player)), player);
        }
    }
}