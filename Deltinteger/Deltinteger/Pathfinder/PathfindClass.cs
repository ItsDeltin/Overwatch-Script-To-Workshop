using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;
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

        public PathmapClass(DeltinScript deltinScript) : base("Pathmap")
        {
            DeltinScript = deltinScript;
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
            Description = "A pathmap can be used for pathfinding.";
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

            staticScope.AddNativeMethod(StopPathfind);
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<IsPathfinding>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<IsPathfindStuck>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<FixPathfind>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<NextNode>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<WalkPath>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<CurrentSegmentAttribute>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<SegmentAttribute>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<RestartThottle>());
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            Nodes = translateInfo.VarCollection.Assign("Nodes", true, false);
            Segments = translateInfo.VarCollection.Assign("Segments", true, false);
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

        // Pathfind(player, destination, [attributes])
        private static readonly FuncMethod Pathfind = new FuncMethodBuilder() {
            Name = "Pathfind",
            Documentation = "Moves the specified player to the destination by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to move."),
                new CodeParameter("destination", "The destination to move the player to."),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(new V_Null()))
            },
            Action = (actionSet, methodCall) => {
                Element player = (Element)methodCall.ParameterValues[0];

                // Store the pathfind destination.
                IndexReference destinationStore = actionSet.VarCollection.Assign("_pathfindDestinationStore", actionSet.IsGlobal, true);
                actionSet.AddAction(destinationStore.SetVariable((Element)methodCall.ParameterValues[1]));

                Element attributes = (Element)methodCall.ParameterValues[2];
                if (attributes is V_Null || attributes is V_EmptyArray) attributes = null;

                DijkstraNormal algorithm = new DijkstraNormal(
                    actionSet, (Element)actionSet.CurrentObject, Element.Part<V_PositionOf>(methodCall.ParameterValues[0]), (Element)destinationStore.GetVariable(), attributes
                );
                algorithm.Get();
                DijkstraBase.Pathfind(
                    actionSet, actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>(), (Element)algorithm.finalPath.GetVariable(), (Element)methodCall.ParameterValues[0], (Element)destinationStore.GetVariable(), (Element)algorithm.finalPathAttributes.GetVariable()
                );

                return null;
            }
        };

        // PathfindAll(players, destination, [attributes])
        private static readonly FuncMethod PathfindAll = new FuncMethodBuilder() {
            Name = "PathfindAll",
            Documentation = "Moves an array of players to the specified position by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The array of players to move."),
                new CodeParameter("destination", "The destination to move the players to."),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(new V_Null()))
            },
            Action = (actionSet, methodCall) => {
                IndexReference destinationStore = actionSet.VarCollection.Assign("_pathfindDestinationStore", actionSet.IsGlobal, true);
                actionSet.AddAction(destinationStore.SetVariable((Element)methodCall.ParameterValues[1]));

                Element attributes = (Element)methodCall.ParameterValues[2];
                if (attributes is V_Null || attributes is V_EmptyArray) attributes = null;

                DijkstraMultiSource algorithm = new DijkstraMultiSource(
                    actionSet, actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>(), (Element)actionSet.CurrentObject, (Element)methodCall.ParameterValues[0], (Element)destinationStore.GetVariable(), attributes
                );
                algorithm.Get();

                return null;
            }
        };

        private static readonly FuncMethod PathfindEither = new FuncMethodBuilder() {
            Name = "PathfindEither",
            Documentation = "Moves a player to the closest position in the destination array by pathfinding.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to pathfind."),
                new CodeParameter("destinations", "The array of destinations."),
                new CodeParameter("attributes", "An array of attributes to pathfind with.", new ExpressionOrWorkshopValue(new V_Null()))
            },
            Action = (actionSet, methodCall) => {
                DijkstraEither algorithm = new DijkstraEither(actionSet, (Element)actionSet.CurrentObject, Element.Part<V_PositionOf>(methodCall.ParameterValues[0]), (Element)methodCall.ParameterValues[1], (Element)methodCall.ParameterValues[2]);
                algorithm.Get();
                DijkstraBase.Pathfind(
                    actionSet, actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>(), (Element)algorithm.finalPath.GetVariable(), (Element)methodCall.ParameterValues[0], algorithm.PointDestination, (Element)algorithm.finalPathAttributes.GetVariable()
                );
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

        private static FuncMethod GetResolve(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "Resolve",
            Documentation = "Resolves all potential paths to the specified destination.",
            DoesReturnValue = true,
            ReturnType = deltinScript.Types.GetInstance<PathResolveClass>(),
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to resolve."),
                new CodeParameter("attributes", "The attributes of the path.")
            },
            Action = (actionSet, call) => {
                ResolveDijkstra resolve = new ResolveDijkstra(actionSet, (Element)call.ParameterValues[0], (Element)call.ParameterValues[1]);
                resolve.Get();
                return resolve.ClassReference.GetVariable();
            }
        };

        // Static functions
        private static FuncMethod StopPathfind = new FuncMethodBuilder() {
            Name = "GetPath",
            Documentation = "Stops pathfinding for the specified players.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players that will stop pathfinding. Can be a single player or an array of players.")
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(
                    actionSet.Translate.DeltinScript.GetComponent<PathfinderInfo>().Path.SetVariable(new V_EmptyArray(), (Element)methodCall.ParameterValues[0])
                );
                return null;
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
}