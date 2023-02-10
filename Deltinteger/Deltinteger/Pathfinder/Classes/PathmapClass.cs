using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Parse.Types.Internal;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapClass : ISelfContainedClass
    {
        static readonly MarkupBuilder AttributesDocumentation = "The attributes to pathfind with. Paths will only be taken if they contain an attribute in the provided array.";

        public string Name => "Pathmap";
        public MarkupBuilder Documentation { get; } = new MarkupBuilder()
            .Add("A pathmap can be used for pathfinding.").NewLine()
            .Add("Pathmaps are imported from ").Code(".pathmap").Add(" files. These files are generated from an ingame editor. Run the ").Code("Copy Pathmap Editor Code").Add(" command by opening the command palette with ").Code("ctrl+shift+p")
            .Add(". Paste the rules into Overwatch and select the map the pathmap will be created for.")
            .ToString();
        public SCClassProvider Provider { get; }
        public CodeType Instance => Provider.Instance;

        private readonly PathfinderTypesComponent _pathfinderTypes;
        private DeltinScript DeltinScript { get; }
        public ITypeVariable Nodes { get; private set; }
        public ITypeVariable Segments { get; private set; }
        public ITypeVariable Attributes { get; private set; }

        private HookVar OnPathStartHook;
        private HookVar OnNodeReachedHook;
        private HookVar OnPathCompleted;
        private HookVar IsNodeReachedDeterminer;
        private HookVar ApplicableNodeDeterminer;
        private PathmapClassConstructor _pathmapClassConstructor;
        private Constructor _emptyConstructor;

        private ITypeSupplier _supplier => DeltinScript.Types;

        public PathmapClass(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;
            Provider = new SCClassProvider(deltinScript, this);
            _pathfinderTypes = deltinScript.GetComponent<PathfinderTypesComponent>();
        }

        public void Setup(SetupSelfContainedClass setup)
        {
            _pathmapClassConstructor = new PathmapClassConstructor(setup.TypeInstance, _supplier);
            _emptyConstructor = new Constructor(setup.TypeInstance, null, AccessLevel.Public)
            {
                Documentation = "Creates an empty pathmap."
            };

            setup.AddConstructor(_pathmapClassConstructor);
            setup.AddConstructor(_emptyConstructor);

            setup.AddObjectMethod(Pathfind);
            setup.AddObjectMethod(PathfindAll);
            setup.AddObjectMethod(GetPath);
            setup.AddObjectMethod(PathfindEither);
            setup.AddObjectMethod(GetResolve(DeltinScript));
            setup.AddObjectMethod(GetResolveTo(DeltinScript));
            setup.AddObjectMethod(AddNode);
            setup.AddObjectMethod(DeleteNode);
            setup.AddObjectMethod(AddSegment);
            setup.AddObjectMethod(DeleteSegment);
            setup.AddObjectMethod(AddAttribute);
            setup.AddObjectMethod(DeleteAttribute);
            setup.AddObjectMethod(DeleteAllAttributes);
            setup.AddObjectMethod(DeleteAllAttributesConnectedToNode);
            setup.AddObjectMethod(SegmentFromNodes);
            setup.AddObjectMethod(Bake);
            setup.AddObjectMethod(BakeCompressed);
            setup.AddObjectMethod(DecompressBakemapData);

            setup.AddStaticMethod(StopPathfind);
            setup.AddStaticMethod(SkipNode);
            setup.AddStaticMethod(CurrentSegmentAttribute);
            setup.AddStaticMethod(NextSegmentAttribute);
            setup.AddStaticMethod(IsPathfinding);
            setup.AddStaticMethod(IsPathfindStuck);
            setup.AddStaticMethod(FixPathfind);
            setup.AddStaticMethod(CurrentPosition);
            setup.AddStaticMethod(NextPosition);
            setup.AddStaticMethod(CurrentNode);
            setup.AddStaticMethod(NextNode);
            setup.AddStaticMethod(ThrottleToNextNode);
            setup.AddStaticMethod(Recalibrate);
            setup.AddStaticMethod(IsPathfindingToNode);
            setup.AddStaticMethod(IsPathfindingToSegment);
            setup.AddStaticMethod(IsPathfindingToAttribute);
            setup.AddStaticMethod(GetCompressedData);

            // Hooks

            // All 'userLambda' variables below should be LambdaAction.

            // Code to run when pathfinding starts.
            OnPathStartHook = new HookVar("OnPathStart", PortableLambdaType.CreateConstantType(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnPathStart = (LambdaAction)userLambda));
            OnPathStartHook.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("The code that runs when a pathfind starts for a player. By default, it will start throttling to the player's current node. Hooking will override the thottle, so if you want to throttle you will need to call ").Code("Pathmap.ThrottleEventPlayerToNextNode").Add(".")
                .NewLine().Add("Call ").Code("EventPlayer()").Add(" to get the player that is pathfinding."));
            // Code to run when node is reached.
            OnNodeReachedHook = new HookVar("OnNodeReached", PortableLambdaType.CreateConstantType(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnNodeReached = (LambdaAction)userLambda));
            OnNodeReachedHook.Documentation = AddHookInfo(new MarkupBuilder().Add("The code that runs when a player reaches a node. Does nothing by default.")
                .NewLine().Add("Call ").Code("EventPlayer()").Add(" to get the player that reached the node."));
            // Code to run when pathfind completes.
            OnPathCompleted = new HookVar("OnPathCompleted", PortableLambdaType.CreateConstantType(), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.OnPathCompleted = (LambdaAction)userLambda));
            OnPathCompleted.Documentation = AddHookInfo(new MarkupBuilder().Add("The code that runs when a player completes a pathfind. By default, it will stop throttling the player and call ").Code("StopPathfind(EventPlayer())").Add(", hooking will override this.")
                .NewLine().Add("Call ").Code("EventPlayer()").Add(" to get the player that completed the path."));
            // The condition to use to determine if a node was reached.
            IsNodeReachedDeterminer = new HookVar("IsNodeReachedDeterminer", PortableLambdaType.CreateConstantType(DeltinScript.Types.Boolean(), new CodeType[] { _supplier.Vector() }), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.IsNodeReachedDeterminer = (LambdaAction)userLambda));
            IsNodeReachedDeterminer.Documentation = AddHookInfo(new MarkupBuilder()
                .Add("The condition that is used to determine if a player reached the current node. The given value is the position of the next node. The returned value should be a boolean determining if the player reached the node they are walking towards.")
                .NewLine()
                .Add("Modify the ").Code("Pathmap.OnNodeReached").Add(" hook to run code when the player reaches the node.")
                .NewSection()
                .Add("By default, it will return true when the player is less than or equal to " + ResolveInfoComponent.DefaultMoveToNext + " meters away from the next node."));
            // The condition to use to determine the closest node to a player.
            ApplicableNodeDeterminer = new HookVar("ApplicableNodeDeterminer", PortableLambdaType.CreateConstantType(DeltinScript.Types.Number(), new CodeType[] { new ArrayType(DeltinScript.Types, _supplier.Vector()), _supplier.Vector() }), userLambda => DeltinScript.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.ApplicableNodeDeterminer = (LambdaAction)userLambda));
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

            setup.AddStaticVariable(OnPathStartHook);
            setup.AddStaticVariable(OnNodeReachedHook);
            setup.AddStaticVariable(OnPathCompleted);
            setup.AddStaticVariable(IsNodeReachedDeterminer);
            setup.AddStaticVariable(ApplicableNodeDeterminer);

            Nodes = setup.AddObjectVariable(new InternalVar("Nodes", _supplier.VectorArray())
            {
                Documentation = "The nodes of the pathmap."
            });
            Segments = setup.AddObjectVariable(new InternalVar("Segments", _supplier.VectorArray())
            {
                Documentation = "The segments of the pathmap. These segments connect the nodes together."
            });
            Attributes = setup.AddObjectVariable(new InternalVar("Attributes", _supplier.VectorArray())
            {
                Documentation = "The attributes of the pathmap. The X of a value in the array is the first node that the attribute is related to. The Y is the second node the attribute is related to. The Z is the attribute's actual value."
            });
        }

        private static MarkupBuilder AddHookInfo(MarkupBuilder markupBuilder) => markupBuilder.NewLine().Add("This is a hook variable, meaning it can only be set at the rule-level.");

        private static ResolveInfoComponent Comp(ActionSet actionSet) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();

        public void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            Element index = (Element)newClassInfo.ObjectReference.GetVariable();

            if (newClassInfo.Constructor == _emptyConstructor)
            {
                Nodes.SetWithReference(actionSet, index, Element.EmptyArray());
                Segments.SetWithReference(actionSet, index, Element.EmptyArray());
                return;
            }

            // Get the pathmap data.
            Pathmap pathMap = (Pathmap)newClassInfo.Parameters[0].AdditionalData;

            Nodes.SetWithReference(actionSet, index, pathMap.NodesAsWorkshopData());
            Segments.SetWithReference(actionSet, index, pathMap.SegmentsAsWorkshopData());
            Attributes.SetWithReference(actionSet, index, pathMap.AttributesAsWorkshopData());
        }

        public Element SegmentsFromNodes(ToWorkshop toWorkshop, IWorkshopTree pathmapObject, Element node1, Element node2) => Element.Filter(
            Segments.GetWithReference(toWorkshop, pathmapObject),
            And(
                Contains(
                    PathfindAlgorithmBuilder.BothNodes(ArrayElement()),
                    node1
                ),
                Contains(
                    PathfindAlgorithmBuilder.BothNodes(ArrayElement()),
                    node2
                )
            )
        );

        private static Element ContainParameter(ActionSet actionSet, string name, IWorkshopTree value)
        {
            IndexReference containParameter = actionSet.VarCollection.Assign(name, actionSet.IsGlobal, true);
            actionSet.AddAction(containParameter.SetVariable((Element)value));
            return (Element)containParameter.GetVariable();
        }

        private readonly static CodeParameter OnLoopStartParameter = new CodeParameter("onLoopStart", $"A list of actions to run at the beginning of the pathfinding code's main loop. This is an optional parameter. By default, it will wait for {Constants.MINIMUM_WAIT} seconds. Manipulate this depending on if speed or server load is more important.", PortableLambdaType.CreateConstantType(), new ExpressionOrWorkshopValue(Element.Null()));
        private readonly static CodeParameter OnNeighborLoopParameter = new CodeParameter("onNeighborLoopStart", $"A list of actions to run at the beginning of the pathfinding code's neighbor loop, which is nested inside the main loop. This is an optional parameter. By default, it will wait for {Constants.MINIMUM_WAIT} seconds. Manipulate this depending on if speed or server load is more important.", PortableLambdaType.CreateConstantType(), new ExpressionOrWorkshopValue(Element.Null()));
        private CodeParameter PrintProgress => new CodeParameter(
            "printProgress",
            new MarkupBuilder().Add("An action that is invoked with the progress of the bake. The value will be between 0 and 1, and will equal 1 when completed.")
                .NewLine().Add("Example usage:").NewLine().StartCodeLine()
                .Add("Pathmap map;").NewLine()
                .Add("map.Bake(printProgress: p => {").NewLine()
                .Indent().Add("// Create a hud text of the baking process.").NewLine()
                .Indent().Add("CreateHudText(AllPlayers(), Header: <\"Baking: <0>\"%, p * 100>, Location: Location.Top);").NewLine()
                .Add("});").EndCodeLine().ToString(),
            PortableLambdaType.CreateConstantType(null, _supplier.Number()), new ExpressionOrWorkshopValue(new EmptyLambda())
        );

        SharedPathfinderInfoValues CreatePathfinderInfo(ActionSet actionSet, Element attributes, IWorkshopTree onLoop, IWorkshopTree onConnectLoop) => new SharedPathfinderInfoValues()
        {
            ActionSet = actionSet,
            PathmapObject = (Element)actionSet.CurrentObject,
            Attributes = attributes,
            OnLoop = onLoop as ILambdaInvocable,
            OnConnectLoop = onConnectLoop as ILambdaInvocable,
            NodeFromPosition = GetNodeFromPositionHandler(actionSet, (Element)actionSet.CurrentObject)
        };

        public INodeFromPosition GetNodeFromPositionHandler(ActionSet actionSet, Element pathmapObject) => ApplicableNodeDeterminer.HookValue == null ?
            (INodeFromPosition)new ClosestNodeFromPosition(actionSet, this, pathmapObject) :
            new NodeFromInvocable(actionSet, this, pathmapObject, (ILambdaInvocable)ApplicableNodeDeterminer.HookValue.Parse(actionSet));

        // Object Functions
        // Pathfind(player, destination, [attributes])
        private FuncMethodBuilder Pathfind => new FuncMethodBuilder()
        {
            Name = "Pathfind",
            Documentation = "Moves the specified player to the destination by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to move.", _supplier.Player()),
                new CodeParameter("destination", "The destination to move the player to.", _supplier.Vector()),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", _supplier.NumberArray(), new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) =>
            {
                Element player = methodCall.Get(0);
                Element destination = ContainParameter(actionSet, "_pathfindDestination", methodCall.ParameterValues[1]); // Store the pathfind destination.

                // Create the pathfinder.
                var pathfindPlayer = new PathfindPlayer(player, destination, CreatePathfinderInfo(actionSet, methodCall.Get(2), methodCall.ParameterValues[3], methodCall.ParameterValues[4]));
                pathfindPlayer.Run();
                return null;
            }
        };

        // PathfindAll(players, destination, [attributes])
        private FuncMethodBuilder PathfindAll => new FuncMethodBuilder()
        {
            Name = "PathfindAll",
            Documentation = "Moves an array of players to the specified position by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The array of players to move.", _supplier.PlayerArray()),
                new CodeParameter("destination", "The destination to move the players to.", _supplier.Vector()),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", _supplier.NumberArray(), new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) =>
            {
                Element players = methodCall.Get(0);
                Element destination = ContainParameter(actionSet, "_pathfindDestination", methodCall.ParameterValues[1]); // Store the pathfind destination.

                var pathfindAll = new PathfindAll(players, destination, CreatePathfinderInfo(actionSet, methodCall.Get(2), methodCall.ParameterValues[3], methodCall.ParameterValues[4]));
                pathfindAll.Run();
                return null;
            }
        };

        // PathfindEither(player, destination, [attributes])
        private FuncMethodBuilder PathfindEither => new FuncMethodBuilder()
        {
            Name = "PathfindEither",
            Documentation = "Moves a player to the closest position in the destination array by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to pathfind.", _supplier.Player()),
                new CodeParameter("destinations", "The array of destinations.", _supplier.VectorArray()),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", _supplier.NumberArray(), new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) =>
            {
                Element player = methodCall.Get(0);
                Element destinations = ContainParameter(actionSet, "_pathfindEitherDestinations", methodCall.ParameterValues[1]);

                var pathfindEither = new PathfindEither(player, destinations, CreatePathfinderInfo(actionSet, methodCall.Get(2), methodCall.ParameterValues[3], methodCall.ParameterValues[4]));
                pathfindEither.Run();
                return null;
            }
        };

        // GetPath()
        private FuncMethodBuilder GetPath => new FuncMethodBuilder()
        {
            Name = "GetPath",
            Documentation = "Returns an array of vectors forming a path from the starting point to the destination.",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The initial position.", _supplier.Vector()),
                new CodeParameter("destination", "The final destination.", _supplier.Vector()),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", _supplier.NumberArray(), new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, methodCall) =>
            {
                Element position = methodCall.Get(0);
                Element destination = ContainParameter(actionSet, "_pathfindDestination", methodCall.ParameterValues[1]);

                var vectorPath = new PathfindVectorPath(position, destination, CreatePathfinderInfo(actionSet, methodCall.Get(2), methodCall.ParameterValues[3], methodCall.ParameterValues[4]));
                vectorPath.Run();
                return vectorPath.Result;
            }
        };

        // Resolve(position, [attributes])
        private FuncMethodBuilder GetResolve(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "Resolve",
            Documentation = "Resolves all potential paths to the specified destination. This can be used to precalculate the path to a position, or to reuse the calculated path to a position.",
            ReturnType = _pathfinderTypes.PathResolve.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve.", _supplier.Vector()),
                new CodeParameter("attributes", "The attributes of the path.", _supplier.NumberArray(), new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, call) =>
            {
                var resolvePath = new ResolvePathfind(call.Get(0), CreatePathfinderInfo(actionSet, call.Get(1), call.ParameterValues[2], call.ParameterValues[3]));
                resolvePath.Run();
                return resolvePath.Result;
            }
        };

        // ResolveTo(position, resolveTo, [attributes])
        private FuncMethodBuilder GetResolveTo(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "ResolveTo",
            Documentation = "Resolves the path to the specified destination. This can be used to precalculate the path to a position, or to reuse the calculated path to a position.",
            ReturnType = _pathfinderTypes.PathResolve.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve.", _supplier.Vector()),
                new CodeParameter("resolveTo", "Resolving will stop once this position is reached.", _supplier.Vector()),
                new CodeParameter("attributes", "The attributes of the path.", _supplier.NumberArray(), new ExpressionOrWorkshopValue(Element.Null())),
                OnLoopStartParameter,
                OnNeighborLoopParameter
            },
            Action = (actionSet, call) =>
            {
                var resolvePath = new ResolvePathfind(call.Get(0), call.Get(1), CreatePathfinderInfo(actionSet, call.Get(2), call.ParameterValues[3], call.ParameterValues[4]));
                resolvePath.Run();
                return resolvePath.Result;
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
        private FuncMethodBuilder AddNode => new FuncMethodBuilder()
        {
            Name = "AddNode",
            Documentation = "Dynamically adds a node to the pathmap.",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to place the new node.", _supplier.Vector())
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) =>
            {
                // Some nodes may be null
                if (actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().PotentiallyNullNodes)
                {
                    Element index = FirstNullOrLength(actionSet, Nodes.Get(actionSet), "Add Node: Index");

                    // Set the position.
                    Nodes.Set(actionSet, methodCall.Get(0), index);

                    // Return the index of the added node.
                    return index;
                }
                else // No nodes will be null.
                {
                    // Append the position.
                    Nodes.Modify(actionSet, Operation.AppendToArray, methodCall.Get(0));

                    // Return the index of the added node.
                    return CountOf(Nodes.Get(actionSet)) - 1;
                }
            }
        };

        // DeleteNode(node)
        private FuncMethodBuilder DeleteNode => new FuncMethodBuilder()
        {
            Name = "DeleteNode",
            Documentation = new MarkupBuilder().Add("Deletes a node from the pathmap using the index of the node. Connected segments are also deleted. This may cause issue for pathfinding players who's path contains the node, so it may be a good idea to use the ").Code("Pathmap.IsPathfindingToNode").Add(" function to check if the node is in their path.")
                .Add("This may also cause issues if this is executed while a pathfinder function is running, like ").Code("Pathmap.Pathfind").Add(" or ").Code("Pathmap.Resolve").Add(".").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("node_index", "The index of the node to remove.", _supplier.Number())
            },
            OnCall = (parseInfo, range) =>
            {
                parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.PotentiallyNullNodes = true);
                return null;
            },
            Action = (actionSet, methodCall) =>
            {
                Nodes.Set(actionSet, Null(), methodCall.Get(0));

                // Delete segments.
                Element connectedSegments = ContainParameter(actionSet, "Delete Node: Segments", Filter(
                    Segments.Get(actionSet),
                    Contains(
                        PathfindAlgorithmBuilder.BothNodes(ArrayElement()),
                        methodCall.ParameterValues[0]
                    )
                ));

                ForeachBuilder loop = new ForeachBuilder(actionSet, connectedSegments);
                Segments.Modify(actionSet, Operation.RemoveFromArrayByValue, loop.IndexValue);
                loop.Finish();

                return null;
            }
        };

        // AddSegment(node_a, node_b)
        private FuncMethodBuilder AddSegment => new FuncMethodBuilder()
        {
            Name = "AddSegment",
            Documentation = "Dynamically connects 2 nodes. Existing path resolves will not reflect the new available path.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The first node of the segment.", _supplier.Number()),
                new CodeParameter("node_b", "The second node of the segment.", _supplier.Number())
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) =>
            {
                Element segmentData = Vector(methodCall.Get(0), methodCall.Get(1), Num(0));

                // Append the vector.
                Segments.Modify(actionSet, Operation.AppendToArray, segmentData);

                // Return the index of the last added node.
                return CountOf(Segments.Get(actionSet)) - 1;
            }
        };

        // DeleteSegment(segment)
        private FuncMethodBuilder DeleteSegment => new FuncMethodBuilder()
        {
            Name = "DeleteSegment",
            Documentation = new MarkupBuilder().Add("Deletes a connection between 2 nodes. This is not destructive, unlike the ").Code("Pathmap.DeleteNode")
                .Add(" counterpart. This can be run while any of the pathfinder functions are running. The change will not reflect for players currently pathfinding.")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("segment", "The segment that will be deleted.", _supplier.Vector())
            },
            Action = (actionSet, methodCall) =>
            {
                Segments.Modify(actionSet, Operation.RemoveFromArrayByValue, methodCall.Get(0));
                return null;
            }
        };

        // AddAttribute(node_a, node_b, attribute)
        private FuncMethodBuilder AddAttribute => new FuncMethodBuilder()
        {
            Name = "AddAttribute",
            Documentation = "Adds an attribute between 2 nodes. This will work even if there is not a segment between the two nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The primary node.", _supplier.Number()),
                new CodeParameter("node_b", "The secondary node.", _supplier.Number()),
                new CodeParameter("attribute", "The attribute value. Should be any number.", _supplier.Number())
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) =>
            {
                Attributes.Modify(actionSet, Operation.AppendToArray, Element.Vector(
                    methodCall.ParameterValues[0],
                    methodCall.ParameterValues[1],
                    methodCall.ParameterValues[2]
                ));
                return null;
            }
        };

        // DeleteAttribute(node_a, node_b, attribute)
        private FuncMethodBuilder DeleteAttribute => new FuncMethodBuilder()
        {
            Name = "DeleteAttribute",
            Documentation = "Removes an attribute between 2 nodes. This will work even if there is not a segment between the two nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The primary node.", _supplier.Number()),
                new CodeParameter("node_b", "The secondary node.", _supplier.Number()),
                new CodeParameter("attribute", "The attribute value that will be removed. Should be any number.", _supplier.Number())
            },
            Action = (actionSet, methodCall) =>
            {
                Attributes.Modify(actionSet, Operation.RemoveFromArrayByValue, Element.Vector(
                    methodCall.ParameterValues[0],
                    methodCall.ParameterValues[1],
                    methodCall.ParameterValues[2]
                ));
                return null;
            }
        };

        // DeleteAllAttributes(node_a, node_b)
        private FuncMethodBuilder DeleteAllAttributes => new FuncMethodBuilder()
        {
            Name = "DeleteAllAttributes",
            Documentation = "Removes all attributes between 2 nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The primary node.", _supplier.Number()),
                new CodeParameter("node_b", "The secondary node.", _supplier.Number())
            },
            Action = (actionSet, methodCall) =>
            {
                Attributes.Modify(
                    actionSet,
                    Operation.RemoveFromArrayByValue,
                    Filter(
                        Attributes.Get(actionSet),
                        And(
                            Compare(methodCall.Get(0), Operator.Equal, XOf(ArrayElement())),
                            Compare(methodCall.Get(1), Operator.Equal, YOf(ArrayElement()))
                        )
                    ));
                return null;
            }
        };

        // DeleteAllAttributesConnectedToNode(node);
        private FuncMethodBuilder DeleteAllAttributesConnectedToNode => new FuncMethodBuilder()
        {
            Name = "DeleteAllAttributesConnectedToNode",
            Documentation = new MarkupBuilder().Add("Removes all attributes connected to a node.").NewLine().Add("This is identical to doing ")
                .Code("ModifyVariable(pathmap.Attributes, Operation.RemoveFromArrayByValue, pathmap.Attributes.FilteredArray(Vector attribute => attribute.X == _node_ || attribute.Y == _node_))")
                .Add(".").ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("node", "Attributes whose node_a or node_b are equal to this will be removed.", _supplier.Number())
            },
            Action = (actionSet, methodCall) =>
            {
                Attributes.Modify(
                    actionSet,
                    Operation.RemoveFromArrayByValue,
                    Filter(
                        Attributes.Get(actionSet),
                        Or(
                            Compare(methodCall.Get(0), Operator.Equal, XOf(ArrayElement())),
                            Compare(methodCall.Get(0), Operator.Equal, YOf(ArrayElement()))
                        )
                    ));
                return null;
            }
        };

        // SegmentFromNodes(node_a, node_b)
        private FuncMethodBuilder SegmentFromNodes => new FuncMethodBuilder()
        {
            Name = "SegmentFromNodes",
            Documentation = "Gets a segment from 2 nodes.",
            Parameters = new CodeParameter[] {
                new CodeParameter("node_a", "The first node index.", _supplier.Number()),
                new CodeParameter("node_b", "The second node index.", _supplier.Number())
            },
            ReturnType = _supplier.Vector(),
            Action = (actionSet, methodCall) => SegmentsFromNodes(actionSet.ToWorkshop, actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], (Element)methodCall.ParameterValues[1])
        };

        // Bake()
        private FuncMethodBuilder Bake => new FuncMethodBuilder()
        {
            Name = "Bake",
            Documentation = new MarkupBuilder().Add("Bakes the pathmap for instant pathfinding. This will block the current rule until the bake is complete.")
                .NewLine().Add("It is recommended to run ").Code("DisableInspectorRecording();").Add(" before baking since it can break the inspector."),
            ReturnType = _pathfinderTypes.Bakemap.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("attributes", AttributesDocumentation, _supplier.NumberArray(), EmptyArray()),
                PrintProgress,
                OnLoopStartParameter
            },
            Action = (actionSet, methodCall) =>
            {
                PathmapBake bake = new PathmapBake(actionSet, (Element)actionSet.CurrentObject, methodCall.Get(0), methodCall.ParameterValues[2] as ILambdaInvocable);
                return bake.Bake(p => ((ILambdaInvocable)methodCall.ParameterValues[1]).Invoke(actionSet, p));
            }
        };

        private FuncMethodBuilder BakeCompressed => new FuncMethodBuilder()
        {
            Name = "BakeCompressed",
            Documentation = new MarkupBuilder().Add("Bakes the pathmap for instant pathfinding. This will block the current rule until the bake is complete.")
                .NewLine().Add("This will execute faster than the ").Code("Bake").Add(" function but will use more elements. Attributes are constant and cannot be changed."),
            ReturnType = _pathfinderTypes.Bakemap.Instance,
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("originalPathmapFile", "The original file of this pathmap.", _supplier),
                new ConstIntegerArrayParameter("attributes", AttributesDocumentation, _supplier, true),
                PrintProgress,
                OnLoopStartParameter
            },
            Action = (actionSet, methodCall) =>
            {
                // Get the pathmap.
                var map = (Pathmap)methodCall.AdditionalParameterData[0];
                var attributes = ((List<int>)methodCall.AdditionalParameterData[1]).ToArray();
                var printProgress = (ILambdaInvocable)methodCall.ParameterValues[2];
                var onLoop = methodCall.ParameterValues[3] as ILambdaInvocable;

                // Get the compressed bakemap.
                var compressed = PathfindUtility.CompressedBakemapFromPathmapAndAttributes(map, attributes);

                return DecompressBakemap(actionSet, compressed, printProgress, onLoop);
            }
        };

        private FuncMethodBuilder GetCompressedData => new FuncMethodBuilder()
        {
            Name = "GetCompressedData",
            Documentation = new MarkupBuilder().Add("Gets the compressed bakemap data. This value can be used with ").Code("Pathmap.DecompressBakemapData").Add(" to get a Bakemap."),
            ReturnType = _supplier.Any(),
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("pathmapFile", "The file of the pathmap to create the bakemap from.", _supplier),
                new ConstIntegerArrayParameter("attributes", AttributesDocumentation, _supplier, true)
            },
            Action = (actionSet, methodCall) =>
            {
                // Get the pathmap.
                var map = (Pathmap)methodCall.AdditionalParameterData[0];
                var attributes = ((List<int>)methodCall.AdditionalParameterData[1]).ToArray();

                // Get the compressed bakemap.
                return PathfindUtility.CompressedBakemapFromPathmapAndAttributes(map, attributes);
            }
        };

        private FuncMethodBuilder DecompressBakemapData => new FuncMethodBuilder()
        {
            Name = "DecompressBakemapData",
            Documentation = new MarkupBuilder().Add("Decompresses bakemap data from ").Code("Pathmap.GetCompressedData").Add(" to get a Bakemap."),
            ReturnType = _pathfinderTypes.Bakemap.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("compressedBakemapData", _supplier.Any()),
                PrintProgress,
                OnLoopStartParameter
            },
            Action = (actionSet, methodCall) =>
            {
                return DecompressBakemap(
                    actionSet,
                    compressedBakemap: methodCall.Get(0),
                    printProgress: methodCall.ParameterValues[1] as ILambdaInvocable,
                    onLoopStart: methodCall.ParameterValues[2] as ILambdaInvocable
                );
            }
        };

        Element DecompressBakemap(ActionSet actionSet, Element compressedBakemap, ILambdaInvocable printProgress, ILambdaInvocable onLoopStart)
        {
            // Get the CompressedBakeComponent.
            var component = actionSet.DeltinScript.GetComponent<CompressedBakeComponent>();

            // Call the decompresser.
            var result = component.WorkshopDecompress(actionSet, compressedBakemap, p => printProgress.Invoke(actionSet, p), onLoopStart);

            // Get the bakemapClass instance.
            var bakemapClass = _pathfinderTypes.Bakemap.Instance;

            // Create a new Bakemap class instance.
            var newBakemap = bakemapClass.Create(actionSet, actionSet.Translate.DeltinScript.GetComponent<ClassData>());
            _pathfinderTypes.Bakemap.Pathmap.SetWithReference(actionSet, newBakemap.Get(), (Element)actionSet.CurrentObject);
            _pathfinderTypes.Bakemap.NodeBake.SetWithReference(actionSet, newBakemap.Get(), result);

            return newBakemap.Get();
        }

        // Static functions
        // StopPathfind(players)
        private FuncMethodBuilder StopPathfind => new FuncMethodBuilder()
        {
            Name = "StopPathfind",
            Documentation = "Stops pathfinding for the specified players.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players that will stop pathfinding. Can be a single player or an array of players.", _supplier.Players())
            },
            Action = (actionSet, methodCall) =>
            {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().StopPathfinding(actionSet, (Element)methodCall.ParameterValues[0]);
                return null;
            }
        };

        // SkipNode(players)
        private FuncMethodBuilder SkipNode => new FuncMethodBuilder()
        {
            Name = "SkipNode",
            Documentation = "Skips the pathfinding player's current node.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The pathfinding player who will skip their current node.", _supplier.Players())
            },
            Action = (actionSet, methodCall) =>
            {
                Comp(actionSet).GetNextNode(actionSet, methodCall.Get(0));
                return null;
            }
        };

        // CurrentSegmentAttribute(player)
        private FuncMethodBuilder CurrentSegmentAttribute => new FuncMethodBuilder()
        {
            Name = "CurrentSegmentAttribute",
            Documentation = "Gets the attribute of the current pathfind segment. If the player is not pathfinding, -1 is returned.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the current segment attribute of.", _supplier.Player())
            },
            ReturnType = new ArrayType(DeltinScript.Types, _supplier.Number()),
            Action = (actionSet, methodCall) => Comp(actionSet).CurrentAttribute.Get((Element)methodCall.ParameterValues[0])
        };

        // NextSegmentAttribute(player)
        private FuncMethodBuilder NextSegmentAttribute => new FuncMethodBuilder()
        {
            Name = "NextSegmentAttribute",
            Documentation = new MarkupBuilder().Add("Gets the attribute of the segment that proceeds the current segment (").Code("Pathmap.CurrentSegmentAttribute").Add(").")
                .NewLine().Add("Once ").Code("Pathmap.CurrentPosition").Add(" is reached, this will become ").Code("Pathmap.CurrentSegmentAttribute").Add(".")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the current segment attribute of.", _supplier.Player())
            },
            ReturnType = new ArrayType(DeltinScript.Types, _supplier.Number()),
            OnCall = (parseInfo, callRange) =>
            {
                parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(c => c.TrackNextAttribute = true);
                return null;
            },
            Action = (actionSet, methodCall) => Comp(actionSet).NextAttribute.Get((Element)methodCall.ParameterValues[0])
        };

        // IsPathfindStuck(player, [speedScalar])
        private FuncMethodBuilder IsPathfindStuck => new FuncMethodBuilder()
        {
            Name = "IsPathfindStuck",
            Documentation = "Returns true if the specified player takes longer than expected to reach the next pathfind node.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check.", _supplier.Player()),
                new CodeParameter(
                    "speedScalar",
                    "The speed scalar of the player. `1` is the default speed of all heroes except Gengi and Tracer, which is `1.1`. Default value is `1`.",
                    _supplier.Number(),
                    new ExpressionOrWorkshopValue(Element.Num(1))
                )
            },
            ReturnType = _supplier.Boolean(),
            OnCall = (parseInfo, docRange) =>
            {
                parseInfo.TranslateInfo.ExecOnComponent<ResolveInfoComponent>(resolveInfo => resolveInfo.TrackTimeSinceLastNode = true);
                return null;
            },
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().IsPathfindingStuck((Element)methodCall.ParameterValues[0], (Element)methodCall.ParameterValues[1])
        };

        // FixPathfind(player)
        private FuncMethodBuilder FixPathfind => new FuncMethodBuilder()
        {
            Name = "FixPathfind",
            Documentation = "Fixes pathfinding for a player by teleporting them to the next node. Use in conjunction with `IsPathfindStuck()`.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to fix pathfinding for.", _supplier.Player())
            },
            Action = (actionSet, methodCall) =>
            {
                Element player = (Element)methodCall.ParameterValues[0];
                actionSet.AddAction(Element.Part("Teleport",
                    player,
                    actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().CurrentPositionWithDestination(player)
                ));
                return null;
            }
        };

        // CurrentPosition(player)
        private FuncMethodBuilder CurrentPosition => new FuncMethodBuilder()
        {
            Name = "CurrentPosition",
            Documentation = "Gets the position the player is currently walking towards.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the current position of.", _supplier.Player())
            },
            ReturnType = _supplier.Vector(),
            Action = (actionSet, methodCall) => Comp(actionSet).CurrentPositionWithDestination(methodCall.Get(0))
        };

        // NextPosition(player)
        private FuncMethodBuilder NextPosition => new FuncMethodBuilder()
        {
            Name = "NextPosition",
            Documentation = new MarkupBuilder().Add("Gets the position of the node that proceeds the current node (").Code("Pathmap.CurrentPosition").Add(").")
                .NewLine().Add("Once ").Code("Pathmap.CurrentPosition").Add(" is reached, this will become ").Code("Pathmap.CurrentPosition").Add(".")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the next position of.", _supplier.Player())
            },
            ReturnType = _supplier.Vector(),
            Action = (actionSet, methodCall) => Comp(actionSet).NextPositionWithDestination(methodCall.Get(0))
        };

        // CurrentNode
        private FuncMethodBuilder CurrentNode => new FuncMethodBuilder()
        {
            Name = "CurrentNode",
            Documentation = "The node index the player is currently walking towards.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the next node of.", _supplier.Player())
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) => Comp(actionSet).Current.Get(methodCall.Get(0))
        };

        // NextNode
        private FuncMethodBuilder NextNode => new FuncMethodBuilder()
        {
            Name = "NextNode",
            Documentation = new MarkupBuilder().Add("Gets the node index that proceeds the current node (").Code("Pathmap.CurrentNode").Add(").")
                .NewLine().Add("Once ").Code("Pathmap.CurrentPosition").Add(" is reached, this will become ").Code("Pathmap.CurrentNode").Add(".")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to get the next node of.", _supplier.Player())
            },
            ReturnType = _supplier.Number(),
            Action = (actionSet, methodCall) =>
            {
                Element player = methodCall.Get(0);
                var comp = Comp(actionSet);
                return comp.ParentArray.Get(player)[comp.Current.Get(player)] - 1;
            }
        };

        // IsPathfinding(player)
        private FuncMethodBuilder IsPathfinding => new FuncMethodBuilder()
        {
            Name = "IsPathfinding",
            Documentation = new MarkupBuilder()
                .Add("Determines if the player is currently pathfinding.").NewLine().Add("This will become ").Code("true").Add(" when any of the pathfinding functions in the pathmap class is used on a player." +
                    " This will remain ").Code("true").Add(" even if the player is dead. If the player reaches their destination or ").Code("Pathmap.StopPathfind").Add(" is called, this will become ").Code("false").Add(".")
                .NewLine()
                .Add("If the player reaches their destination, ").Code("Pathmap.OnPathCompleted").Add(" will run immediately after this becomes ").Code("false").Add(".")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The target player to determine if pathfinding.", _supplier.Player())
            },
            ReturnType = _supplier.Boolean(),
            Action = (actionSet, methodCall) => actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().IsPathfinding((Element)methodCall.ParameterValues[0])
        };

        // ThrottleEventPlayerToNextNode
        private FuncMethodBuilder ThrottleToNextNode => new FuncMethodBuilder()
        {
            Name = "ThrottleEventPlayerToNextNode",
            Documentation = new MarkupBuilder().Add("Throttles the event player to the next node in their path. This is called by default when the player starts a pathfind, but if the ")
                .Code("Pathmap.OnPathStart").Add(" hook is overridden, then this will need to be called in the hook unless you want to change how the player navigates to the next position")
                .ToString(),
            Action = (actionSet, methodCall) =>
            {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().ThrottleEventPlayerToNextNode(actionSet);
                return null;
            }
        };

        private FuncMethodBuilder Recalibrate => new FuncMethodBuilder()
        {
            Name = "Recalibrate",
            Documentation = new MarkupBuilder().Add("Specified players will get the closest node and restart the path from there. This is useful when used in conjuction with ")
                .Code("Pathmap.Resolve").Add(" and the players have a chance of being knocked off the path into another possible path.")
                .ToString(),
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players that will recalibrate their pathfinding.", _supplier.Players())
            },
            Action = (actionSet, methodCall) =>
            {
                actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>().SetCurrent(actionSet, (Element)methodCall.ParameterValues[0]);
                return null;
            }
        };

        private FuncMethodBuilder IsPathfindingToNode => new FuncMethodBuilder()
        {
            Name = "IsPathfindingToNode",
            Documentation = "Determines if a player is pathfinding towards a node. This will return true if the node is anywhere in their path, not just the one they are currently walking towards.",
            ReturnType = _supplier.Boolean(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check.", _supplier.Player()),
                new CodeParameter("node_index", "The node to check. This is the index of the node in the pathmap's Node array.", _supplier.Number())
            },
            Action = (actionSet, methodCall) => new IsTravelingToNode((Element)methodCall.ParameterValues[1]).Get(actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>(), actionSet, (Element)methodCall.ParameterValues[0])
        };

        private FuncMethodBuilder IsPathfindingToSegment => new FuncMethodBuilder()
        {
            Name = "IsPathfindingToSegment",
            Documentation = "Determines if a player is pathfinding towards a node. This will return true if the segment is anywhere in their path, not just the one they are currently walking towards.",
            ReturnType = _supplier.Boolean(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check.", _supplier.Player()),
                new CodeParameter("segment", "The segment to check. This is not an index of the pathmap's segment array, instead it is the segment itself.", _supplier.Any())
            },
            Action = (actionSet, methodCall) => new IsTravelingToSegment((Element)methodCall.ParameterValues[1]).Get(actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>(), actionSet, (Element)methodCall.ParameterValues[0])
        };

        private FuncMethodBuilder IsPathfindingToAttribute => new FuncMethodBuilder()
        {
            Name = "IsPathfindingToAttribute",
            Documentation = new MarkupBuilder().Add("Determines if a player is pathfinding towards an attribute.")
                .Add(" This will return true if the attribute is anywhere in their path, not just the one they are currently walking towards.")
                .Add(" This will not return true if the attribute is on the segment the player is currently walking on, instead for this case use ").Code("CurrentSegmentAttribute").Add(".")
                .ToString(),
            ReturnType = _supplier.Boolean(),
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to check.", _supplier.Player()),
                new CodeParameter("attribute", "The segment to check.", _supplier.Number())
            },
            Action = (actionSet, methodCall) => new IsTravelingToAttribute((Element)methodCall.ParameterValues[1]).Get(actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>(), actionSet, (Element)methodCall.ParameterValues[0])
        };
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(CodeType instance, ITypeSupplier types) : base(instance, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("pathmapFile", "File path of the pathmap to use. Must be a `.pathmap` file.", types)
            };
            Documentation = "Creates a pathmap from a `.pathmap` file.";
        }

        public override void Parse(ActionSet actionSet, WorkshopParameter[] parameters) => throw new NotImplementedException();
    }

    class PathmapFileParameter : FileParameter
    {
        public PathmapFileParameter(string parameterName, string description, ITypeSupplier types) : base(parameterName, description, types, ".pathmap") { }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            string filepath = base.Validate(parseInfo, value, valueRange, additionalData) as string;
            if (filepath == null) return null;

            Pathmap map;
            try
            {
                map = GetFile<PathmapLoader>(parseInfo, filepath, uri => new PathmapLoader(uri)).Pathmap;
            }
            catch (Exception ex)
            {
                parseInfo.Script.Diagnostics.Error("Failed to deserialize the Pathmap: " + ex.Message, valueRange);
                return null;
            }

            if (map.Nodes == null)
            {
                parseInfo.Script.Diagnostics.Error("Pathmap has no Nodes value", valueRange);
                return null;
            }

            if (map.Segments == null)
            {
                parseInfo.Script.Diagnostics.Error("Pathmap has no Segments value", valueRange);
                return null;
            }

            if (map.Attributes == null)
            {
                parseInfo.Script.Diagnostics.Error("Pathmap has no Attributes value", valueRange);
                return null;
            }

            parseInfo.TranslateInfo.ExecOnComponent<CompressedBakeComponent>(component => component.SetNodesValue(map.Nodes.Length));

            return map;
        }
    }
}