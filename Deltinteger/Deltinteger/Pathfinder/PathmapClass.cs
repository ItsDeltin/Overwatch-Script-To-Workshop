using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapClass : SelfContainedClassProvider
    {
        private DeltinScript DeltinScript { get; }
        public IndexReference Nodes { get; private set; }
        public IndexReference Segments { get; private set; }
        public IndexReference Attributes { get; private set; }

        private InternalVar NodesVar;
        private InternalVar SegmentsVar;
        private InternalVar AttributesVar;
        private HookVar OnPathStartHook;
        private HookVar OnNodeReachedHook;
        private HookVar OnPathCompleted;
        private HookVar IsNodeReachedDeterminer;
        private HookVar ApplicableNodeDeterminer;

        private ITypeSupplier _supplier => DeltinScript.Types;

        public PathmapClass(DeltinScript deltinScript) : base("Pathmap")
        {
            DeltinScript = deltinScript;
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this),
                new Constructor(WorkingInstance, null, AccessLevel.Public) {
                    Documentation = "Creates an empty pathmap."
                }
            };
            Documentation = new MarkupBuilder()
                .Add("A pathmap can be used for pathfinding.").NewLine()
                .Add("Pathmaps are imported from ").Code(".pathmap").Add(" files. These files are generated from an ingame editor. Run the ").Code("Copy Pathmap Editor Code").Add(" command by opening the command palette with ").Code("ctrl+shift+p")
                .Add(". Paste the rules into Overwatch and select the map the pathmap will be created for.")
                .ToString();
        }

        public override bool BuiltInTypeMatches(Type type) => false;

        public override void ResolveElements()
        {
            if (_elementsResolved) return;
            base.ResolveElements();

            ObjectScope.AddNativeMethod(Pathfind);
            ObjectScope.AddNativeMethod(PathfindAll);
            ObjectScope.AddNativeMethod(GetPath);
            ObjectScope.AddNativeMethod(PathfindEither);
            ObjectScope.AddNativeMethod(GetResolve(DeltinScript));
            ObjectScope.AddNativeMethod(GetResolveTo(DeltinScript));
            ObjectScope.AddNativeMethod(AddNode);
            ObjectScope.AddNativeMethod(DeleteNode);
            ObjectScope.AddNativeMethod(AddSegment);
            ObjectScope.AddNativeMethod(DeleteSegment);
            ObjectScope.AddNativeMethod(AddAttribute);
            ObjectScope.AddNativeMethod(DeleteAttribute);
            ObjectScope.AddNativeMethod(DeleteAllAttributes);
            ObjectScope.AddNativeMethod(DeleteAllAttributesConnectedToNode);
            ObjectScope.AddNativeMethod(SegmentFromNodes);

            StaticScope.AddNativeMethod(StopPathfind);
            StaticScope.AddNativeMethod(CurrentSegmentAttribute);
            StaticScope.AddNativeMethod(IsPathfinding);
            StaticScope.AddNativeMethod(IsPathfindStuck);
            StaticScope.AddNativeMethod(FixPathfind);
            StaticScope.AddNativeMethod(NextPosition);
            StaticScope.AddNativeMethod(CurrentNode);
            StaticScope.AddNativeMethod(ThrottleToNextNode);
            StaticScope.AddNativeMethod(Recalibrate);
            StaticScope.AddNativeMethod(IsPathfindingToNode);
            StaticScope.AddNativeMethod(IsPathfindingToSegment);
            StaticScope.AddNativeMethod(IsPathfindingToAttribute);

            // Hooks

            // All 'userLambda' variables below should be LambdaAction.

            // Code to run when pathfinding starts.
            OnPathStartHook = new HookVar("OnPathStart", new BlockLambda(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnPathStart = (LambdaAction)userLambda));
            OnPathStartHook.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("The code that runs when a pathfind starts for a player. By default, it will start throttling to the player's current node. Hooking will override the thottle, so if you want to throttle you will need to call ").Code("Pathmap.ThrottleEventPlayerToNextNode").Add(".")
                .NewLine().Add("Call ").Code("EventPlayer()").Add(" to get the player that is pathfinding."));
            // Code to run when node is reached.
            OnNodeReachedHook = new HookVar("OnNodeReached", new BlockLambda(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnNodeReached = (LambdaAction)userLambda));
            OnNodeReachedHook.Documentation = AddHookInfo(new MarkupBuilder().Add("The code that runs when a player reaches a node. Does nothing by default.")
                .NewLine().Add("Call ").Code("EventPlayer()").Add(" to get the player that reached the node."));
            // Code to run when pathfind completes.
            OnPathCompleted = new HookVar("OnPathCompleted", new BlockLambda(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnPathCompleted = (LambdaAction)userLambda));
            OnPathCompleted.Documentation = AddHookInfo(new MarkupBuilder().Add("The code that runs when a player completes a pathfind. By default, it will stop throttling the player and call ").Code("StopPathfind(EventPlayer())").Add(", hooking will override this.")
                .NewLine().Add("Call ").Code("EventPlayer()").Add(" to get the player that completed the path."));
            // The condition to use to determine if a node was reached.
            IsNodeReachedDeterminer = new HookVar("IsNodeReachedDeterminer", new MacroLambda(DeltinScript.Types.Boolean(), new CodeType[] {_supplier.Vector()}), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.IsNodeReachedDeterminer = (LambdaAction)userLambda));
            IsNodeReachedDeterminer.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("The condition that is used to determine if a player reached the current node. The given value is the position of the next node. The returned value should be a boolean determining if the player reached the node they are walking towards.")
                .NewLine()
                .Add("Modify the ").Code("Pathmap.OnNodeReached").Add(" hook to run code when the player reaches the node.")
                .NewSection()
                .Add("By default, it will return true when the player is less than or equal to " + ResolveInfoComponent.DefaultMoveToNext + " meters away from the next node."));
            // The condition to use to determine the closest node to a player.
            ApplicableNodeDeterminer = new HookVar("ApplicableNodeDeterminer", new ValueBlockLambda(DeltinScript.Types.Number(), new CodeType[] { new ArrayType(DeltinScript.Types, _supplier.Vector()), _supplier.Vector() }), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.ApplicableNodeDeterminer = (LambdaAction)userLambda));
            ApplicableNodeDeterminer.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("Gets a node that is relevent to the specified position. Hooking this will change how OSTW generated rules will get the node. By default, it will return the node that is closest to the specified position.")
                .NewLine()
                .Add("The returned value must be the index of the node in the ").Code("nodes").Add(" array.")
                .NewLine()
                .Add("The default implementation may cause problems if the closest node to a player is behind a wall or under the floor. Hooking this so line-of-sight is accounted for may be a good idea if accuracy is more important than server load, for example:")
                .NewSection()
                .StartCodeLine()
                .Add(@"Pathmap.ApplicableNodeDeterminer = (Vector[] nodes, Vector position) => {
    return IndexOfArrayValue(nodes, nodes.FilteredArray(Vector node => node.IsInLineOfSight(position)).SortedArray(Vector node => node.DistanceTo(position))[0]);
}")
                .EndCodeLine());

            StaticScope.AddNativeVariable(OnPathStartHook);
            StaticScope.AddNativeVariable(OnNodeReachedHook);
            StaticScope.AddNativeVariable(OnPathCompleted);
            StaticScope.AddNativeVariable(IsNodeReachedDeterminer);
            StaticScope.AddNativeVariable(ApplicableNodeDeterminer);

            NodesVar = new InternalVar("Nodes") {
                Documentation = "The nodes of the pathmap.",
                CodeType = new ArrayType(DeltinScript.Types, _supplier.Vector())
            };
            SegmentsVar = new InternalVar("Segments") {
                Documentation = "The segments of the pathmap. These segments connect the nodes together.",
                CodeType = new ArrayType(DeltinScript.Types, SegmentsStruct.Instance)
            };
            AttributesVar = new InternalVar("Attributes") {
                Documentation = "The attributes of the pathmap. The X of a value in the array is the first node that the attribute is related to. The Y is the second node the attribute is related to. The Z is the attribute's actual value.",
                CodeType = new ArrayType(DeltinScript.Types, _supplier.Vector())
            };
            ObjectScope.AddNativeVariable(NodesVar);
            ObjectScope.AddNativeVariable(SegmentsVar);
            ObjectScope.AddNativeVariable(AttributesVar);
        }

        private static MarkupBuilder AddHookInfo(MarkupBuilder markupBuilder) => markupBuilder.NewLine().Add("This is a hook variable, meaning it can only be set at the rule-level.");

        private static ResolveInfoComponent Comp(ActionSet actionSet) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            Nodes = translateInfo.VarCollection.Assign("Nodes", true, false);
            Segments = translateInfo.VarCollection.Assign("Segments", true, false);
            Attributes = translateInfo.VarCollection.Assign("Attributes", true, false);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            base.AddObjectVariablesToAssigner(reference, assigner);

            // Add hooks to assigner.
            AddHook(assigner, OnPathStartHook);
            AddHook(assigner, OnNodeReachedHook);
            AddHook(assigner, OnPathCompleted);
            AddHook(assigner, IsNodeReachedDeterminer);

            assigner.Add(NodesVar, Nodes.CreateChild((Element)reference));
            assigner.Add(SegmentsVar, Segments.CreateChild((Element)reference));
            assigner.Add(AttributesVar, Attributes.CreateChild((Element)reference));
        }

        private void AddHook(VarIndexAssigner assigner, HookVar hook)
        {
            if (hook.WasSet) assigner.Add(hook, (IWorkshopTree)hook.HookValue);
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            Element index = (Element)newClassInfo.ObjectReference.GetVariable();

            if (newClassInfo.AdditionalParameterData.Length == 0)
            {
                actionSet.AddAction(Nodes.SetVariable(Element.EmptyArray(), index: index));
                actionSet.AddAction(Segments.SetVariable(Element.EmptyArray(), index: index));
                return;
            }

             // Get the pathmap data.
            Pathmap pathMap = (Pathmap)newClassInfo.AdditionalParameterData[0];
            
            IndexReference nodes = actionSet.VarCollection.Assign("_tempNodes", actionSet.IsGlobal, false);
            IndexReference segments = actionSet.VarCollection.Assign("_tempSegments", actionSet.IsGlobal, false);
            IndexReference attributes = actionSet.VarCollection.Assign("_tempAttributes", actionSet.IsGlobal, false);

            actionSet.AddAction(nodes.SetVariable(Element.EmptyArray()));
            actionSet.AddAction(segments.SetVariable(Element.EmptyArray()));
            actionSet.AddAction(attributes.SetVariable(Element.EmptyArray()));

            foreach (var node in pathMap.Nodes) actionSet.AddAction(nodes.ModifyVariable(operation: Operation.AppendToArray, value: node.ToVector()));
            foreach (var segment in pathMap.Segments) actionSet.AddAction(segments.ModifyVariable(operation: Operation.AppendToArray, value: segment.AsWorkshopData()));
            foreach (var attribute in pathMap.Attributes) actionSet.AddAction(attributes.ModifyVariable(operation: Operation.AppendToArray, value: attribute.AsWorkshopData()));
            
            actionSet.AddAction(Nodes.SetVariable((Element)nodes.GetVariable(), index: index));
            actionSet.AddAction(Segments.SetVariable((Element)segments.GetVariable(), index: index));
            actionSet.AddAction(Attributes.SetVariable((Element)attributes.GetVariable(), index: index));
        }

        public Element SegmentsFromNodes(IWorkshopTree pathmapObject, Element node1, Element node2) => Element.Filter(
            Segments.Get()[(Element)pathmapObject],
            Element.And(
                Element.Contains(
                    DijkstraBase.BothNodes(Element.ArrayElement()),
                    node1
                ),
                Element.Contains(
                    DijkstraBase.BothNodes(Element.ArrayElement()),
                    node2
                )
            )
        );

        private void SetHooks(DijkstraBase algorithm, IWorkshopTree onLoop, IWorkshopTree onConnectLoop)
        {
            // OnLoop
            if (onLoop is LambdaAction onLoopLambda)
                algorithm.OnLoop = actionSet => onLoopLambda.Invoke(actionSet);
            
            // OnConnectLoop
            if (onConnectLoop is LambdaAction onConnectLoopLambda)
                algorithm.OnConnectLoop = actionSet => onConnectLoopLambda.Invoke(actionSet);
            
            if (ApplicableNodeDeterminer.HookValue != null)
                algorithm.GetClosestNode = (actionSet, nodes, position) => (Element)((LambdaAction)ApplicableNodeDeterminer.HookValue).Invoke(actionSet, nodes, position);
        }

        private static Element ContainParameter(ActionSet actionSet, string name, IWorkshopTree value)
        {
            IndexReference containParameter = actionSet.VarCollection.Assign(name, actionSet.IsGlobal, true);
            actionSet.AddAction(containParameter.SetVariable((Element)value));
            return (Element)containParameter.GetVariable();
        }

        private readonly static CodeParameter OnLoopStartParameter = new CodeParameter("onLoopStart", $"A list of actions to run at the beginning of the pathfinding code's main loop. This is an optional parameter. By default, it will wait for {Constants.MINIMUM_WAIT} seconds. Manipulate this depending on if speed or server load is more important.", new BlockLambda(), new ExpressionOrWorkshopValue());
        private readonly static CodeParameter OnNeighborLoopParameter = new CodeParameter("onNeighborLoopStart", $"A list of actions to run at the beginning of the pathfinding code's neighbor loop, which is nested inside the main loop. This is an optional parameter. By default, it will wait for {Constants.MINIMUM_WAIT} seconds. Manipulate this depending on if speed or server load is more important.", new BlockLambda(), new ExpressionOrWorkshopValue());

        // Object Functions
        // Pathfind(player, destination, [attributes])
        private FuncMethod Pathfind => new FuncMethodBuilder() {
            Name = "Pathfind",
            Documentation = "Moves the specified player to the destination by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to move."),
                new CodeParameter("destination", "The destination to move the player to."),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) => {
                Element player = (Element)methodCall.ParameterValues[0];

                // Store the pathfind destination.
                Element destination = ContainParameter(actionSet, "_pathfindDestinationStore", methodCall.ParameterValues[1]);

                DijkstraPlayer algorithm = new DijkstraPlayer(actionSet, (Element)actionSet.CurrentObject, player, destination, (Element)methodCall.ParameterValues[2]);

                // Set lambda hooks
                SetHooks(algorithm, methodCall.ParameterValues[3], methodCall.ParameterValues[4]);

                // Apply
                algorithm.Get();
                return null;
            }
        };

        // PathfindAll(players, destination, [attributes])
        private FuncMethod PathfindAll => new FuncMethodBuilder() {
            Name = "PathfindAll",
            Documentation = "Moves an array of players to the specified position by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The array of players to move."),
                new CodeParameter("destination", "The destination to move the players to."),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(workshopValue:null)),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) => {
                Element destination = ContainParameter(actionSet, "_pathfindDestinationStore", methodCall.ParameterValues[1]);

                DijkstraMultiSource algorithm = new DijkstraMultiSource(actionSet, (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], destination, (Element)methodCall.ParameterValues[2]);

                // Set lambda hooks
                SetHooks(algorithm, methodCall.ParameterValues[3], methodCall.ParameterValues[4]);

                // Apply
                algorithm.Get();
                return null;
            }
        };

        // PathfindEither(player, destination, [attributes])
        private FuncMethod PathfindEither => new FuncMethodBuilder() {
            Name = "PathfindEither",
            Documentation = "Moves a player to the closest position in the destination array by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to pathfind."),
                new CodeParameter("destinations", "The array of destinations."),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) => {
                Element destinations = ContainParameter(actionSet, "_pathfindDestinationStore", methodCall.ParameterValues[1]);

                DijkstraEither algorithm = new DijkstraEither(actionSet, (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], destinations, (Element)methodCall.ParameterValues[2]);

                // Set lambda hooks
                SetHooks(algorithm, methodCall.ParameterValues[3], methodCall.ParameterValues[4]);

                // Apply
                algorithm.Get();
                return null;
            }
        };

        // GetPath()
        private static FuncMethod GetPath = new FuncMethodBuilder() {
            Name = "GetPath",
            Documentation = "Returns an array of vectors forming a path from the starting point to the destination.",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The initial position."),
                new CodeParameter("destination", "The final destination.")
            },
            Action = (actionSet, methodCall) => {
                Element destination = ContainParameter(actionSet, "_pathfindDestinationStore", methodCall.ParameterValues[1]);

                DijkstraNormal algorithm = new DijkstraNormal(actionSet, (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], destination, null);
                algorithm.Get();

                return Element.Append(algorithm.finalPath.GetVariable(), destination);
            }
        };

        // Resolve(position, [attributes])
        private FuncMethod GetResolve(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "Resolve",
            Documentation = "Resolves all potential paths to the specified destination. This can be used to precalculate the path to a position, or to reuse the calculated path to a position.",
            ReturnType = deltinScript.Types.GetInstanceFromInitializer<PathResolveClass>(),
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve."),
                new CodeParameter("attributes", "The attributes of the path.", new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, call) => {
                ResolveDijkstra resolve = new ResolveDijkstra(actionSet, (Element)call.ParameterValues[0], (Element)call.ParameterValues[1]);

                // Set lambda hooks
                SetHooks(resolve, call.ParameterValues[2], call.ParameterValues[3]);

                // Apply
                resolve.Get();
                return resolve.ClassReference.GetVariable();
            }
        };

        // ResolveTo(position, resolveTo, [attributes])
        private static FuncMethod GetResolveTo(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ResolveTo",
            Documentation = "Resolves the path to the specified destination. This can be used to precalculate the path to a position, or to reuse the calculated path to a position.",
            ReturnType = deltinScript.Types.GetInstanceFromInitializer<PathResolveClass>(),
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve."),
                new CodeParameter("resolveTo", "Resolving will stop once this position is reached."),
                new CodeParameter("attributes", "The attributes of the path.", new ExpressionOrWorkshopValue(Element.Null()))
            },
            Action = (actionSet, call) => {
                ResolveDijkstra resolve = new ResolveDijkstra(actionSet, (Element)call.ParameterValues[0], ContainParameter(actionSet, "_pathfindDestinationStore", call.ParameterValues[1]), (Element)call.ParameterValues[2]);
                resolve.Get();
                return resolve.ClassReference.GetVariable();
            }
        };

        private Element FirstNullOrLength(ActionSet actionSet, Element array, string tempVariableName)
        {
            // Get the index of the first null node.
            IndexReference index = actionSet.VarCollection.Assign(tempVariableName, actionSet.IsGlobal, true);
            
            // Get the first null value.
            actionSet.AddAction(index.SetVariable(Element.IndexOfArrayValue(array, Element.Null())));

            // If the index is -1, use the count of the element.
            actionSet.AddAction(index.SetVariable(Element.TernaryConditional(Element.Compare(index.Get(), Operator.Equal, Element.Num(-1)), Element.CountOf(array), index.Get())));

            // Done
            return index.Get();
        }

        // AddNode(position)
        private FuncMethod AddNode => new FuncMethodBuilder() {
            Name = "AddNode",
            Documentation = "Dynamically adds a node to the pathmap.",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to place the new node.")
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) => {
                // Some nodes may be null
                if (actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().PotentiallyNullNodes)
                {
                    Element index = FirstNullOrLength(actionSet, Nodes.Get()[(Element)actionSet.CurrentObject], "Add Node: Index");

                    // Set the position.
                    actionSet.AddAction(Nodes.SetVariable((Element)methodCall.ParameterValues[0], null, (Element)actionSet.CurrentObject, index));
                    
                    // Return the index of the added node.
                    return index;
                }
                else // No nodes will be null.
                {
                    // Append the position.
                    actionSet.AddAction(Nodes.ModifyVariable(operation: Operation.AppendToArray, value: (Element)methodCall.ParameterValues[0], index: (Element)actionSet.CurrentObject));
                    
                    // Return the index of the added node.
                    return Element.CountOf(Nodes.Get()[(Element)actionSet.CurrentObject]) - 1;
                }
            }
        };

        // DeleteNode(node)
        private FuncMethod DeleteNode => new FuncMethodBuilder() {
            Name = "DeleteNode",
            Documentation = new MarkupBuilder().Add("Deletes a node from the pathmap using the index of the node. Connected segments are also deleted. This may cause issue for pathfinding players who's path contains the node, so it may be a good idea to use the ").Code("Pathmap.IsPathfindingToNode").Add(" function to check if the node is in their path.")
                .Add("This may also cause issues if this is executed while a pathfinder function is running, like ").Code("Pathmap.Pathfind").Add(" or ").Code("Pathmap.Resolve").Add(".").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("node_index", "The index of the node to remove.")
            },
            OnCall = (parseInfo, range) => parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.PotentiallyNullNodes = true),
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Nodes.SetVariable(value: Element.Null(), index: new Element[] { (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0] }));

                // Delete segments.
                Element connectedSegments = ContainParameter(actionSet, "Delete Node: Segments", Element.Filter(
                    Segments.Get()[(Element)actionSet.CurrentObject],
                    Element.Contains(
                        DijkstraBase.BothNodes(Element.ArrayElement()),
                        methodCall.ParameterValues[0]
                    )
                ));

                ForeachBuilder loop = new ForeachBuilder(actionSet, connectedSegments);
                actionSet.AddAction(Segments.ModifyVariable(Operation.RemoveFromArrayByValue, value:loop.IndexValue, index:(Element)actionSet.CurrentObject));
                loop.Finish();

                return null;
            }
        };

        // AddSegment(node_a, node_b)
        private FuncMethod AddSegment => new FuncMethodBuilder() {
            Name = "AddSegment",
            Documentation = "Dynamically connects 2 nodes. Existing path resolves will not reflect the new available path.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The first node of the segment."),
                new CodeParameter("node_b", "The second node of the segment.")
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) => {                
                Element segmentData = Element.Vector((Element)methodCall.ParameterValues[0], (Element)methodCall.ParameterValues[1], Element.Num(0));
                
                // Append the vector.
                actionSet.AddAction(Segments.ModifyVariable(operation: Operation.AppendToArray, value: segmentData, index: (Element)actionSet.CurrentObject));

                // Return the index of the last added node.
                return Element.CountOf(Segments.GetVariable()) - 1;
            }
        };

        // DeleteSegment(segment)
        private FuncMethod DeleteSegment => new FuncMethodBuilder() {
            Name = "DeleteSegment",
            Documentation = new MarkupBuilder().Add("Deletes a connection between 2 nodes. This is not destructive, unlike the ").Code("Pathmap.DeleteNode").Add(" counterpart. This can be run while any of the pathfinder functions are running. The change will not reflect for players currently pathfinding.").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("segment", "The segment that will be deleted.", SegmentsStruct.Instance)
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Segments.ModifyVariable(Operation.RemoveFromArrayByValue, value: (Element)methodCall.ParameterValues[0], index: (Element)actionSet.CurrentObject));
                return null;
            }
        };

        // AddAttribute(node_a, node_b, attribute)
        private FuncMethod AddAttribute => new FuncMethodBuilder() {
            Name = "AddAttribute",
            Documentation = "Adds an attribute between 2 nodes. This will work even if there is not a segment between the two nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The primary node."),
                new CodeParameter("node_b", "The secondary node."),
                new CodeParameter("attribute", "The attribute value. Should be any number.")
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Attributes.ModifyVariable(Operation.AppendToArray, Element.Vector(
                    methodCall.ParameterValues[0],
                    methodCall.ParameterValues[1],
                    methodCall.ParameterValues[2]
                ), null, (Element)actionSet.CurrentObject));
                return null;
            }
        };

        // DeleteAttribute(node_a, node_b, attribute)
        private FuncMethod DeleteAttribute => new FuncMethodBuilder() {
            Name = "DeleteAttribute",
            Documentation = "Removes an attribute between 2 nodes. This will work even if there is not a segment between the two nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The primary node."),
                new CodeParameter("node_b", "The secondary node."),
                new CodeParameter("attribute", "The attribute value that will be removed. Should be any number.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Attributes.ModifyVariable(Operation.RemoveFromArrayByValue, Element.Vector(
                    methodCall.ParameterValues[0],
                    methodCall.ParameterValues[1],
                    methodCall.ParameterValues[2]
                ), null, (Element)actionSet.CurrentObject));
                return null;
            }
        };

        // DeleteAllAttributes(node_a, node_b)
        private FuncMethod DeleteAllAttributes => new FuncMethodBuilder() {
            Name = "DeleteAllAttributes",
            Documentation = "Removes all attributes between 2 nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The primary node."),
                new CodeParameter("node_b", "The secondary node.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Attributes.ModifyVariable(
                    Operation.RemoveFromArrayByValue,
                    Element.Filter(
                        Attributes.Get()[(Element)actionSet.CurrentObject],
                        Element.And(
                            Element.Compare(methodCall.ParameterValues[0], Operator.Equal, Element.XOf(Element.ArrayElement())),
                            Element.Compare(methodCall.ParameterValues[1], Operator.Equal, Element.YOf(Element.ArrayElement()))
                        )
                    ),
                    index: (Element)actionSet.CurrentObject)
                );
                return null;
            }
        };

        // DeleteAllAttributesConnectedToNode(node);
        private FuncMethod DeleteAllAttributesConnectedToNode => new FuncMethodBuilder() {
            Name = "DeleteAllAttributesConnectedToNode",
            Documentation = new MarkupBuilder().Add("Removes all attributes connected to a node.").NewLine().Add("This is identical to doing ")
                .Code("ModifyVariable(pathmap.Attributes, Operation.RemoveFromArrayByValue, pathmap.Attributes.FilteredArray(Vector attribute => attribute.X == _node_ || attribute.Y == _node_))")
                .Add(".").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("node", "Attributes whose node_a or node_b are equal to this will be removed.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Attributes.ModifyVariable(
                    Operation.RemoveFromArrayByValue,
                    Element.Filter(
                        Attributes.Get()[(Element)actionSet.CurrentObject],
                        Element.Or(
                            Element.Compare(methodCall.ParameterValues[0], Operator.Equal, Element.XOf(Element.ArrayElement())),
                            Element.Compare(methodCall.ParameterValues[0], Operator.Equal, Element.YOf(Element.ArrayElement()))
                        )
                    ),
                    index: (Element)actionSet.CurrentObject)
                );
                return null;
            }
        };

        // SegmentFromNodes(node_a, node_b)
        private FuncMethod SegmentFromNodes => new FuncMethodBuilder() {
            Name = "SegmentFromNodes",
            Documentation = "Gets a segment from 2 nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The first node index."),
                new CodeParameter("node_b", "The second node index.")
            },
            ReturnType = SegmentsStruct.Instance,
            Action = (actionSet, methodCall) => SegmentsFromNodes(actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], (Element)methodCall.ParameterValues[1])
        };

        // Static functions
        // StopPathfind(players)
        private FuncMethod StopPathfind => new FuncMethodBuilder() {
            Name = "StopPathfind",
            Documentation = "Stops pathfinding for the specified players.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players that will stop pathfinding. Can be a single player or an array of players.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().StopPathfinding(actionSet, (Element)methodCall.ParameterValues[0]);
                return null;
            }
        };
    
        // CurrentSegmentAttribute(player)
        private FuncMethod CurrentSegmentAttribute => new FuncMethodBuilder() {
            Name = "CurrentSegmentAttribute",
            Documentation = "Gets the attribute of the current pathfind segment. If the player is not pathfinding, -1 is returned.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the current segment attribute of.")
            },
            ReturnType = new ArrayType(DeltinScript.Types, _supplier.Number()),
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentAttribute.Get((Element)methodCall.ParameterValues[0])
        };

        // IsPathfindStuck(player, [speedScalar])
        private FuncMethod IsPathfindStuck => new FuncMethodBuilder() {
            Name = "IsPathfindStuck",
            Documentation = "Returns true if the specified player takes longer than expected to reach the next pathfind node.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check."),
                new CodeParameter(
                    "speedScalar",
                    "The speed scalar of the player. `1` is the default speed of all heroes except Gengi and Tracer, which is `1.1`. Default value is `1`.",
                    new ExpressionOrWorkshopValue(Element.Num(1))
                )
            },
            ReturnType = BooleanType.Instance,
            OnCall = (parseInfo, docRange) => { parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.TrackTimeSinceLastNode = true); },
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().IsPathfindingStuck((Element)methodCall.ParameterValues[0], (Element)methodCall.ParameterValues[1])
        };

        // FixPathfind(player)
        private FuncMethod FixPathfind => new FuncMethodBuilder() {
            Name = "FixPathfind",
            Documentation = "Fixes pathfinding for a player by teleporting them to the next node. Use in conjunction with `IsPathfindStuck()`.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to fix pathfinding for.")
            },
            Action = (actionSet, methodCall) => {
                Element player = (Element)methodCall.ParameterValues[0];
                actionSet.AddAction(Element.Part("Teleport",
                    player,
                    actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentPositionWithDestination(player)
                ));
                return null;
            }
        };

        // NextPosition(player)
        private FuncMethod NextPosition => new FuncMethodBuilder() {
            Name = "NextPosition",
            Documentation = "Gets the position the player is currently walking towards.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the next position of.")
            },
            ReturnType = _supplier.Vector(),
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentPositionWithDestination((Element)methodCall.ParameterValues[0])
        };

        // CurrentNode
        private FuncMethod CurrentNode => new FuncMethodBuilder() {
            Name = "CurrentNode",
            Documentation = "The node index the player is currently walking towards.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the next node of.")
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) => Comp(actionSet).Current.Get(methodCall.Get(0))
        };

        // IsPathfinding(player)
        private FuncMethod IsPathfinding => new FuncMethodBuilder() {
            Name = "IsPathfinding",
            Documentation = new MarkupBuilder()
                .Add("Determines if the player is currently pathfinding.").NewLine().Add("This will become ").Code("true").Add(" when any of the pathfinding functions in the pathmap class is used on a player." + 
                    " This will remain ").Code("true").Add(" even if the player is dead. If the player reaches their destination or ").Code("Pathmap.StopPathfind").Add(" is called, this will become ").Code("false").Add(".")
                .NewLine()
                .Add("If the player reaches their destination, ").Code("Pathmap.OnPathCompleted").Add(" will run immediately after this becomes ").Code("false").Add(".")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The target player to determine if pathfinding.")
            },
            ReturnType = BooleanType.Instance,
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().IsPathfinding((Element)methodCall.ParameterValues[0])
        };
    
        // ThrottleEventPlayerToNextNode
        private FuncMethod ThrottleToNextNode => new FuncMethodBuilder() {
            Name = "ThrottleEventPlayerToNextNode",
            Documentation = new MarkupBuilder().Add("Throttles the event player to the next node in their path. This is called by default when the player starts a pathfind, but if the ").Code("Pathmap.OnPathStart").Add(" hook is overridden, then this will need to be called in the hook unless you want to change how the player navigates to the next position").ToString(),
            Action = (actionSet, methodCall) => {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().ThrottleEventPlayerToNextNode(actionSet);
                return null;
            }
        };

        private FuncMethod Recalibrate => new FuncMethodBuilder() {
            Name = "Recalibrate",
            Documentation = new MarkupBuilder().Add("Specified players will get the closest node and restart the path from there. This is useful when used in conjuction with ").Code("Pathmap.Resolve").Add(" and the players have a chance of being knocked off the path into another possible path.").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players that will recalibrate their pathfinding.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().SetCurrent(actionSet, (Element)methodCall.ParameterValues[0]);
                return null;
            }
        };
    
        private FuncMethod IsPathfindingToNode => new FuncMethodBuilder() {
            Name = "IsPathfindingToNode",
            Documentation = "Determines if a player is pathfinding towards a node. This will return true if the node is anywhere in their path, not just the one they are currently walking towards.",
            ReturnType = BooleanType.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check."),
                new CodeParameter("node_index", "The node to check. This is the index of the node in the pathmap's Node array.")
            },
            Action = (actionSet, methodCall) => new IsTravelingToNode((Element)methodCall.ParameterValues[1]).Get(actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>(), actionSet, (Element)methodCall.ParameterValues[0])
        };

        private FuncMethod IsPathfindingToSegment => new FuncMethodBuilder() {
            Name = "IsPathfindingToSegment",
            Documentation = "Determines if a player is pathfinding towards a node. This will return true if the segment is anywhere in their path, not just the one they are currently walking towards.",
            ReturnType = BooleanType.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check."),
                new CodeParameter("segment", "The segment to check. This is not an index of the pathmap's segment array, instead it is the segment itself.")
            },
            Action = (actionSet, methodCall) => new IsTravelingToSegment((Element)methodCall.ParameterValues[1]).Get(actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>(), actionSet, (Element)methodCall.ParameterValues[0])
        };

        private FuncMethod IsPathfindingToAttribute => new FuncMethodBuilder() {
            Name = "IsPathfindingToAttribute",
            Documentation = new MarkupBuilder().Add("Determines if a player is pathfinding towards an attribute.")
                .Add(" This will return true if the attribute is anywhere in their path, not just the one they are currently walking towards.")
                .Add(" This will not return true if the attribute is on the segment the player is currently walking on, instead for this case use ").Code("CurrentSegmentAttribute").Add(".")
                .ToString(),
            ReturnType = BooleanType.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check."),
                new CodeParameter("attribute", "The segment to check.")
            },
            Action = (actionSet, methodCall) => new IsTravelingToAttribute((Element)methodCall.ParameterValues[1]).Get(actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>(), actionSet, (Element)methodCall.ParameterValues[0])
        };
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(PathmapClass pathMapClass) : base(pathMapClass.WorkingInstance, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("pathmapFile", "File path of the pathmap to use. Must be a `.pathmap` file.")
            };
            Documentation = "Creates a pathmap from a `.pathmap` file.";
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData) => throw new NotImplementedException();
    }

    class PathmapFileParameter : FileParameter
    {
        public PathmapFileParameter(string parameterName, string description) : base(parameterName, description, ".pathmap") {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            string filepath = base.Validate(parseInfo, value, valueRange) as string;
            if (filepath == null) return null;

            Pathmap map;
            try
            {
                map = Pathmap.ImportFromFile(filepath);
            }
            catch (Exception ex)
            {
                parseInfo.Script.Diagnostics.Error("Failed to deserialize the Pathmap: " + ex.Message, valueRange);
                return null;
            }

            return map;
        }
    }

    class SegmentsStruct : CodeType
    {
        public static readonly SegmentsStruct Instance = new SegmentsStruct();
        private readonly InternalVar Node_A;
        private readonly InternalVar Node_B;
        private readonly Scope _scope = new Scope();

        private SegmentsStruct() : base("PathmapSegment")
        {
            this.Kind = "struct";

            Node_A = new InternalVar("Node_A", CompletionItemKind.Property) { Documentation = "The primary node of this segment. This returns a number which is the index of the node in the pathmap." };
            Node_B = new InternalVar("Node_B", CompletionItemKind.Property) { Documentation = "The secondary node of this segment. This returns a number which is the index of the node in the pathmap." };
            _scope.AddNativeVariable(Node_A);
            _scope.AddNativeVariable(Node_B);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(Node_A, DijkstraBase.Node1((Element)reference));
            assigner.Add(Node_B, DijkstraBase.Node2((Element)reference));
        }

        public override Scope GetObjectScope() => _scope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = "Segments",
            Kind = CompletionItemKind.Struct
        };
    }
}