using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;
using Deltin.Deltinteger.Parse.Lambda;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapClass : ClassType
    {
        private DeltinScript DeltinScript { get; }
        public IndexReference Nodes { get; private set; }
        public IndexReference Segments { get; private set; }

        private InternalVar NodesVar;
        private InternalVar SegmentsVar;
        private HookVar OnPathStartHook;
        private HookVar OnNodeReachedHook;
        private HookVar OnPathCompleted;
        private HookVar IsNodeReachedDeterminer;
        private HookVar ApplicableNodeDeterminer;

        public PathmapClass(DeltinScript deltinScript) : base("Pathmap")
        {
            DeltinScript = deltinScript;
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
            Description = new MarkupBuilder()
                .Add("A pathmap can be used for pathfinding.").NewLine()
                .Add("Pathmaps are imported from ").Code(".pathmap").Add(" files. These files are generated from an ingame editor. Run the ").Code("Copy Pathmap Editor Code").Add(" command by opening the command palette with ").Code("ctrl+shift+p")
                .Add(". Paste the rules into Overwatch and select the map the pathmap will be created for.")
                .ToString();
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            serveObjectScope.AddNativeMethod(Pathfind);
            serveObjectScope.AddNativeMethod(PathfindAll);
            serveObjectScope.AddNativeMethod(GetPath);
            serveObjectScope.AddNativeMethod(PathfindEither);
            serveObjectScope.AddNativeMethod(GetResolve(DeltinScript));
            serveObjectScope.AddNativeMethod(GetResolveTo(DeltinScript));
            serveObjectScope.AddNativeMethod(AddNode);
            serveObjectScope.AddNativeMethod(DeleteNode);
            serveObjectScope.AddNativeMethod(AddSegment);
            serveObjectScope.AddNativeMethod(DeleteSegment);
            serveObjectScope.AddNativeMethod(SetSegmentAttributeAB);
            serveObjectScope.AddNativeMethod(SetSegmentAttributeBA);

            staticScope.AddNativeMethod(StopPathfind);
            staticScope.AddNativeMethod(CurrentSegmentAttribute);
            staticScope.AddNativeMethod(IsPathfinding);
            staticScope.AddNativeMethod(IsPathfindStuck);
            staticScope.AddNativeMethod(FixPathfind);
            staticScope.AddNativeMethod(NextNode);
            staticScope.AddNativeMethod(ThrottleToNextNode);

            // Hooks

            // All 'userLambda' variables below should be LambdaAction.

            // Code to run when pathfinding starts.
            OnPathStartHook = new HookVar("OnPathStart", new BlockLambda(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnPathStart = (LambdaAction)userLambda));
            OnPathStartHook.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("The code that runs when a pathfind starts for a player. By default, it will start throttling to the player's current node. Hooking will override the thottle, so if you want to throttle you will need to call ").Code("Pathmap.ThrottleEventPlayerToNextNode").Add("."));
            // Code to run when node is reached.
            OnNodeReachedHook = new HookVar("OnNodeReached", new BlockLambda(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnNodeReached = (LambdaAction)userLambda));
            OnNodeReachedHook.Documentation = AddHookInfo(new MarkupBuilder().Add("The code that runs when a player reaches a node. Does nothing by default."));
            // Code to run when pathfind completes.
            OnPathCompleted = new HookVar("OnPathCompleted", new BlockLambda(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnPathCompleted = (LambdaAction)userLambda));
            OnPathCompleted.Documentation = AddHookInfo(new MarkupBuilder().Add("The code that runs when a player completes a pathfind. By default, it will stop throttling the player. Hooking will override the stop throttle."));
            // The condition to use to determine if a node was reached.
            IsNodeReachedDeterminer = new HookVar("IsNodeReachedDeterminer", new MacroLambda(null, new CodeType[] {VectorType.Instance}), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.IsNodeReachedDeterminer = (LambdaAction)userLambda));
            IsNodeReachedDeterminer.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("The condition that is used to determine if a player reached the current node. The given value is the position of the next node. The returned value should be a boolean determining if the player reached the node they are walking towards.")
                .NewLine()
                .Add("Modify the ").Code("Pathmap.OnNodeReached").Add(" hook to run code when the player reaches the node.")
                .NewSection()
                .Add("By default, it will return true when the player is less than or equal to " + ResolveInfoComponent.DefaultMoveToNext + " meters away from the next node."));
            // The condition to use to determine the closest node to a player.
            ApplicableNodeDeterminer = new HookVar("ApplicableNodeDeterminer", new ValueBlockLambda(null, new CodeType[] { new ArrayType(VectorType.Instance), VectorType.Instance }), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.ApplicableNodeDeterminer = (LambdaAction)userLambda));
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

            staticScope.AddNativeVariable(OnPathStartHook);
            staticScope.AddNativeVariable(OnNodeReachedHook);
            staticScope.AddNativeVariable(OnPathCompleted);
            staticScope.AddNativeVariable(IsNodeReachedDeterminer);
            staticScope.AddNativeVariable(ApplicableNodeDeterminer);

            NodesVar = new InternalVar("Nodes", CompletionItemKind.Property) {
                Documentation = "The nodes of the pathmap.",
                CodeType = new ArrayType(VectorType.Instance),
                IsSettable = false
            };
            SegmentsVar = new InternalVar("Segments", CompletionItemKind.Property) {
                Documentation = "The segments of the pathmap. These segments connect the nodes together.",
                CodeType = new ArrayType(SegmentsStruct.Instance),
                IsSettable = false
            };
            serveObjectScope.AddNativeVariable(NodesVar);
            serveObjectScope.AddNativeVariable(SegmentsVar);
        }

        private static MarkupBuilder AddHookInfo(MarkupBuilder markupBuilder) => markupBuilder.NewLine().Add("This is a hook variable, meaning it can only be set at the rule-level.");

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            Nodes = translateInfo.VarCollection.Assign("Nodes", true, false);
            Segments = translateInfo.VarCollection.Assign("Segments", true, false);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            base.AddObjectVariablesToAssigner(reference, assigner);

            // Add hooks to assigner.
            AddHook(assigner, OnPathStartHook);
            AddHook(assigner, OnNodeReachedHook);
            AddHook(assigner, OnPathCompleted);
            AddHook(assigner, IsNodeReachedDeterminer);

            assigner.Add(NodesVar, Nodes.GetVariable());
            assigner.Add(SegmentsVar, Segments.GetVariable());
        }

        private void AddHook(VarIndexAssigner assigner, HookVar hook)
        {
            if (hook.WasSet) assigner.Add(hook, (IWorkshopTree)hook.HookValue);
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Get the pathmap data.
            PathMap pathMap = (PathMap)newClassInfo.AdditionalParameterData[0];

            Element index = (Element)newClassInfo.ObjectReference.GetVariable();
            IndexReference nodes = actionSet.VarCollection.Assign("_tempNodes", actionSet.IsGlobal, false);
            IndexReference segments = actionSet.VarCollection.Assign("_tempSegments", actionSet.IsGlobal, false);

            actionSet.AddAction(nodes.SetVariable(new V_EmptyArray()));
            actionSet.AddAction(segments.SetVariable(new V_EmptyArray()));

            foreach (var node in pathMap.Nodes)
                actionSet.AddAction(nodes.ModifyVariable(operation: Operation.AppendToArray, value: node.ToVector()));
            foreach (var segment in pathMap.Segments)
                actionSet.AddAction(segments.ModifyVariable(operation: Operation.AppendToArray, value: segment.AsWorkshopData()));
            
            actionSet.AddAction(Nodes.SetVariable((Element)nodes.GetVariable(), index: index));
            actionSet.AddAction(Segments.SetVariable((Element)segments.GetVariable(), index: index));
        }

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
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(new V_Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) => {
                Element player = (Element)methodCall.ParameterValues[0];

                // Store the pathfind destination.
                Element destination = ContainParameter(actionSet, "_pathfindDestinationStore", methodCall.ParameterValues[1]);

                // Get the attributes.
                Element attributes = (Element)methodCall.ParameterValues[2];
                if (attributes is V_Null || attributes is V_EmptyArray) attributes = null;

                DijkstraPlayer algorithm = new DijkstraPlayer(actionSet, (Element)actionSet.CurrentObject, player, destination, attributes);

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
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(new V_Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) => {
                Element destination = ContainParameter(actionSet, "_pathfindDestinationStore", methodCall.ParameterValues[1]);

                Element attributes = (Element)methodCall.ParameterValues[2];
                if (attributes is V_Null || attributes is V_EmptyArray) attributes = null;

                DijkstraMultiSource algorithm = new DijkstraMultiSource(
                    actionSet, (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], destination, attributes
                );

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
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(new V_Null())),
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
                IndexReference destinationStore = actionSet.VarCollection.Assign("_pathfindDestinationStore", actionSet.IsGlobal, true);
                actionSet.AddAction(destinationStore.SetVariable((Element)methodCall.ParameterValues[1]));

                DijkstraNormal algorithm = new DijkstraNormal(
                    actionSet, (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], (Element)destinationStore.GetVariable(), null
                );
                algorithm.Get();

                return Element.Part<V_Append>(algorithm.finalPath.GetVariable(), destinationStore.GetVariable());
            }
        };

        // Resolve(position, [attributes])
        private FuncMethod GetResolve(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "Resolve",
            Documentation = "Resolves all potential paths to the specified destination.",
            DoesReturnValue = true,
            ReturnType = deltinScript.Types.GetInstance<PathResolveClass>(),
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve."),
                new CodeParameter("attributes", "The attributes of the path.", new ExpressionOrWorkshopValue(new V_Null())),
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
            Documentation = "Resolves the path to the specified destination.",
            DoesReturnValue = true,
            ReturnType = deltinScript.Types.GetInstance<PathResolveClass>(),
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve."),
                new CodeParameter("resolveTo", "Resolving will stop once this position is reached."),
                new CodeParameter("attributes", "The attributes of the path.", new ExpressionOrWorkshopValue(new V_Null()))
            },
            Action = (actionSet, call) => {
                ResolveDijkstra resolve = new ResolveDijkstra(actionSet, (Element)call.ParameterValues[0],  (Element)call.ParameterValues[1], (Element)call.ParameterValues[2]);
                resolve.Get();
                return resolve.ClassReference.GetVariable();
            }
        };

        private Element FirstNullOrLength(ActionSet actionSet, Element array, string tempVariableName)
        {
            // Get the index of the first null node.
            IndexReference index = actionSet.VarCollection.Assign(tempVariableName, actionSet.IsGlobal, true);
            
            // Get the first null value.
            index.SetVariable(Element.Part<V_IndexOfArrayValue>(array, new V_Null()));

            // If the index is -1, use the count of the element.
            index.SetVariable(Element.TernaryConditional(new V_Compare(index.Get(), Operators.Equal, new V_Number(-1)), Element.Part<V_CountOf>(array), index.Get()));

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
            DoesReturnValue = true,
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
                    return Element.Part<V_CountOf>(Nodes.Get()[(Element)actionSet.CurrentObject]);
                }
            }
        };

        // DeleteNode(node)
        private FuncMethod DeleteNode => new FuncMethodBuilder() {
            Name = "DeleteNode",
            Documentation = new MarkupBuilder().Add("Deletes a node from the pathmap using the index of the node. Connected segments are also deleted. This may cause issue for pathfinding players who's path contains the node, so it may be a good idea to use the ").Code("Pathmap.IsPathfindingToNode").Add(" function to check if the node is in their path.").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("node", "The index of the node to remove.")
            },
            OnCall = (parseInfo, range) => parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.PotentiallyNullNodes = true),
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Nodes.SetVariable(value: new V_Null(), index: new Element[] { (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0] }));

                // Delete segments.
                Element connectedSegments = ContainParameter(actionSet, "Delete Node: Segments", Element.Part<V_FilteredArray>(
                    Segments.Get()[(Element)actionSet.CurrentObject],
                    Element.Part<V_ArrayContains>(
                        DijkstraBase.BothNodes(new V_ArrayElement()),
                        methodCall.ParameterValues[0]
                    )
                ));

                ForeachBuilder loop = new ForeachBuilder(actionSet, connectedSegments);
                actionSet.AddAction(Segments.ModifyVariable(Operation.RemoveFromArrayByValue, value:loop.IndexValue, index:(Element)actionSet.CurrentObject));
                loop.Finish();

                return null;
            }
        };

        // AddSegment(node_a, node_b, [attribute_ab], [attribute_ba])
        private FuncMethod AddSegment => new FuncMethodBuilder() {
            Name = "AddSegment",
            Documentation = "Dynamically connects 2 nodes. Existing path resolves will not reflect the new available path.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The first node of the segment."),
                new CodeParameter("node_b", "The second node of the segment."),
                new CodeParameter("attribute_ab", "The attribute when travelling from a to b.", new ExpressionOrWorkshopValue()),
                new CodeParameter("attribute_ba", "The attribute when travelling from b to a.", new ExpressionOrWorkshopValue())
            },
            DoesReturnValue = true,
            Action = (actionSet, methodCall) => {
                Element x, y, z = new V_Number(0);

                // Encode the x, y, and z values.
                if (ExpressionOrWorkshopValue.UseNonnullParameter(methodCall.ParameterValues[2])) // A value was set for the 'attribute_ab' parameter.
                    x = ((Element)methodCall.ParameterValues[0]) + (((Element)methodCall.ParameterValues[2]) / 100);
                else // A value was not set for the 'attribute_ab' parameter.
                    x = (Element)methodCall.ParameterValues[0];
                
                // Do the same with y
                if (ExpressionOrWorkshopValue.UseNonnullParameter(methodCall.ParameterValues[3])) // A value was set for the 'attribute_ba' parameter.
                    y = ((Element)methodCall.ParameterValues[1]) + (((Element)methodCall.ParameterValues[3]) / 100);
                else // A value was not set for the 'attribute_ba' parameter.
                    y = (Element)methodCall.ParameterValues[1];
                
                V_Vector segmentData = new V_Vector(x, y, z);
                
                // Append the vector.
                actionSet.AddAction(Segments.ModifyVariable(operation: Operation.AppendToArray, value: segmentData, index: (Element)actionSet.CurrentObject));

                // Return the index of the last added node.
                return Element.Part<V_CountOf>(Segments.GetVariable()) - 1;
            }
        };

        // DeleteSegment(segment)
        private FuncMethod DeleteSegment => new FuncMethodBuilder() {
            Name = "DeleteSegment",
            Documentation = new MarkupBuilder().Add("Deletes a connection between 2 nodes. This is not destructive, unlike the ").Code("Pathmap.DeleteNode").Add(" counterpart. This can be run while any of the pathfinder functions are running. The change will not reflect for players currently pathfinding.").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("segment", "The segment that will be deleted.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Segments.ModifyVariable(Operation.RemoveFromArrayByValue, value: (Element)methodCall.ParameterValues[0], index: (Element)actionSet.CurrentObject));
                return null;
            }
        };

        // SetSegmentAttributeAB(segment, attribute)
        private FuncMethod SetSegmentAttributeAB => new FuncMethodBuilder() {
            Name = "SetSegmentAttributeAB",
            Documentation = "Sets the primary segment attribute when travelling from node A to B.",
            Parameters = new CodeParameter[] {
                new CodeParameter("segment", "The index of the segment that is being modified."),
                new CodeParameter("attribute", "The new A to B attribute of the segment.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Segments.SetVariable(
                    index: new Element[] {
                        (Element)actionSet.CurrentObject,
                        (Element)methodCall.ParameterValues[0]
                    },
                    value: ((Element)methodCall.ParameterValues[1]) + (Segments.Get()[(Element)actionSet.CurrentObject][(Element)methodCall.ParameterValues[0]] % 1)
                ));
                return null;
            }
        };

        // SetSegmentAttributeBA(segment, attribute)
        private FuncMethod SetSegmentAttributeBA => new FuncMethodBuilder() {
            Name = "SetSegmentAttributeBA",
            Documentation = "Sets the primary segment attribute when travelling from node B to A.",
            Parameters = new CodeParameter[] {
                new CodeParameter("segment", "The index of the segment that is being modified."),
                new CodeParameter("attribute", "The new B to A attribute of the segment.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Segments.SetVariable(
                    index: new Element[] {
                        (Element)actionSet.CurrentObject,
                        (Element)methodCall.ParameterValues[0]
                    },
                    value: Element.Part<V_RoundToInteger>(Segments.Get()[(Element)actionSet.CurrentObject][(Element)methodCall.ParameterValues[0]], EnumData.GetEnumValue(Rounding.Down)) + (Element)methodCall.ParameterValues[1]
                ));
                return null;
            }
        };

        // Static functions
        // StopPathfind(players)
        private static FuncMethod StopPathfind = new FuncMethodBuilder() {
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
        private static FuncMethod CurrentSegmentAttribute = new FuncMethodBuilder() {
            Name = "CurrentSegmentAttribute",
            Documentation = "Gets the attribute of the current pathfind segment. If the player is not pathfinding, -1 is returned.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the current segment attribute of.")
            },
            DoesReturnValue = true,
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentAttribute.Get((Element)methodCall.ParameterValues[0])
        };

        // IsPathfindStuck(player, [speedScalar])
        private static FuncMethod IsPathfindStuck = new FuncMethodBuilder() {
            Name = "IsPathfindStuck",
            Documentation = "Returns true if the specified player takes longer than expected to reach the next pathfind node.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check."),
                new CodeParameter(
                    "speedScalar",
                    "The speed scalar of the player. `1` is the default speed of all heroes except Gengi and Tracer, which is `1.1`. Default value is `1`.",
                    new ExpressionOrWorkshopValue(new V_Number(1))
                )
            },
            DoesReturnValue = true,
            OnCall = (parseInfo, docRange) => { parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.TrackTimeSinceLastNode = true); },
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().IsPathfindingStuck((Element)methodCall.ParameterValues[0], (Element)methodCall.ParameterValues[1])
        };

        // FixPathfind(player)
        private static FuncMethod FixPathfind = new FuncMethodBuilder() {
            Name = "FixPathfind",
            Documentation = "Fixes pathfinding for a player by teleporting them to the next node. Use in conjunction with `IsPathfindStuck()`.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to fix pathfinding for.")
            },
            Action = (actionSet, methodCall) => {
                Element player = (Element)methodCall.ParameterValues[0];
                actionSet.AddAction(Element.Part<A_Teleport>(
                    player,
                    actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentPositionWithDestination(player)
                ));
                return null;
            }
        };

        // NextNode(player)
        private static FuncMethod NextNode = new FuncMethodBuilder() {
            Name = "NextNode",
            Documentation = "Gets the position the player is currently walking towards.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the next node of.")
            },
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentPositionWithDestination((Element)methodCall.ParameterValues[0])
        };

        // IsPathfinding(player)
        private static FuncMethod IsPathfinding = new FuncMethodBuilder() {
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
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().IsPathfinding((Element)methodCall.ParameterValues[0])
        };
    
        // ThrottleEventPlayerToNextNode
        private static FuncMethod ThrottleToNextNode = new FuncMethodBuilder() {
            Name = "ThrottleEventPlayerToNextNode",
            Documentation = new MarkupBuilder().Add("Throttles the event player to the next node in their path. This is called by default when the player starts a pathfind, but if the ").Code("Pathmap.OnPathStart").Add(" hook is overridden, then this will need to be called in the hook unless you want to change how the player navigates to the next position").ToString(),
            Action = (actionSet, methodCall) => {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().ThrottleEventPlayerToNextNode(actionSet);
                return null;
            }
        };
    
        private static FuncMethod IsPathfindingToNode = new FuncMethodBuilder() {
            Name = "IsPathfindingToNode",
            Documentation = "Determines if a player is pathfinding towards a node. This will return true if the node is anywhere in their path, not just the one they are currently walking towards.",
            DoesReturnValue = true,
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check."),
                new CodeParameter("node", "The node to check.")
            },
            Action = (actionSet, methodCall) => {
                IndexReference result = actionSet.VarCollection.Assign("Lookahead: Result", actionSet.IsGlobal, true);
                actionSet.AddAction(result.SetVariable(new V_False()));

                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().Lookahead(actionSet, (Element)methodCall.ParameterValues[0], Element.Part<V_Not>(result.Get()), lookahead => {
                    actionSet.AddAction(result.SetVariable(new V_Compare(lookahead.Node, Operators.Equal, methodCall.ParameterValues[1])));
                });

                return result.Get();
            }
        };
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(PathmapClass pathMapClass) : base(pathMapClass, null, AccessLevel.Public)
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

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            string filepath = base.Validate(script, value, valueRange) as string;
            if (filepath == null) return null;

            PathMap map;
            try
            {
                map = PathMap.ImportFromXMLFile(filepath);
            }
            catch (InvalidOperationException)
            {
                script.Diagnostics.Error("Failed to deserialize the PathMap.", valueRange);
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
        private readonly InternalVar Attribute_AB;
        private readonly InternalVar Attribute_BA;
        private readonly Scope _scope = new Scope();

        private SegmentsStruct() : base("PathmapSegment")
        {
            this.Kind = "struct";

            Node_A = new InternalVar("Node_A", CompletionItemKind.Property) { Documentation = "The primary node of this segment. This returns a number which is the index of the node in the pathmap." };
            Node_B = new InternalVar("Node_B", CompletionItemKind.Property) { Documentation = "The secondary node of this segment. This returns a number which is the index of the node in the pathmap." };
            Attribute_AB = new InternalVar("Attribute_AB", CompletionItemKind.Property) { Documentation = "The attribute of this segment when travelling from node A to B." };
            Attribute_BA = new InternalVar("Attribute_BA", CompletionItemKind.Property) { Documentation = "The attribute of this segment when travelling from node B to A." };

            _scope.AddNativeVariable(Node_A);
            _scope.AddNativeVariable(Node_B);
            _scope.AddNativeVariable(Attribute_AB);
            _scope.AddNativeVariable(Attribute_BA);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(Node_A, DijkstraBase.Node1((Element)reference));
            assigner.Add(Node_B, DijkstraBase.Node2((Element)reference));
            assigner.Add(Attribute_AB, DijkstraBase.Node1Attribute((Element)reference));
            assigner.Add(Attribute_BA, DijkstraBase.Node2Attribute((Element)reference));
        }

        public override Scope GetObjectScope() => _scope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = "Segments",
            Kind = CompletionItemKind.Struct
        };
    }
}