using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    using Walker;

    static class BakemapStruct
    {
        public const string NODES_VAR_NAME = "Nodes";
        public const string ATTRIBUTES_VAR_NAME = "Attributes";
        public const string BAKE_VAR_NAME = "Bake";

        public static SCStructProvider Create(DeltinScript deltinScript)
        {
            ITypeSupplier types = deltinScript.Types;

            return SelfContainedType.Create("BakemapStruct", "todo", setup =>
            {
                var nodesVariable = setup.AddObjectVariable(NODES_VAR_NAME, types.Vector());
                var attributesVariable = setup.AddObjectVariable(ATTRIBUTES_VAR_NAME, types.Any());
                var bakeVariable = setup.AddObjectVariable(BAKE_VAR_NAME, types.Any());

                setup.AddObjectMethod(new FuncMethodBuilder()
                {
                    Name = "Pathfind",
                    Documentation = "Pathfinds specified players to the destination.",
                    Parameters = new CodeParameter[] {
                        new CodeParameter("players", "The players to pathfind.", types.Players()),
                        new CodeParameter("destination", "The position to pathfind to.", types.Vector())
                    },
                    Action = (actionSet, methodCall) =>
                    {
                        var nodeArray = nodesVariable.Get(actionSet);
                        var players = methodCall.Get(0);
                        var destination = methodCall.Get(1);

                        var destinationNode = PathfindUtility.GetBakemapTargetNode(nodeArray, destination);

                        var pathExecutorComponent = actionSet.DeltinScript.GetComponent<PathExecutorComponent>();
                        var pathExecutor = setup.Match(
                            isClass: () => pathExecutorComponent.ClassExcecutor(actionSet.CurrentObject),
                            isStruct: () => pathExecutorComponent.StructExecutor(actionSet.CurrentObject)
                        );

                        var parentArray = bakeVariable.Get(actionSet)[destinationNode];

                        pathExecutor.Pathfind(actionSet, players, parentArray, destination);
                        return null;
                    }
                });

                setup.AddStaticMethod(new FuncMethodBuilder()
                {
                    Name = "Load",
                    Documentation = "todo",
                    Parameters = new CodeParameter[] {
                        new PathmapFileParameter("originalPathmapFile", "The original file of this pathmap.", types),
                        new ConstIntegerArrayParameter("attributes", "todo", types, true),
                    },
                    ReturnType = setup.TypeInstance,
                    Action = (actionSet, methodCall) =>
                    {
                        // Get the pathmap.
                        var map = (Pathmap)methodCall.AdditionalParameterData[0];
                        var attributes = ((List<int>)methodCall.AdditionalParameterData[1]).ToArray();

                        var baked = PathfindUtility.DecompressPathmap(actionSet, map, attributes);

                        return setup.CreateInstanceWithValues(
                            actionSet,
                            map.NodesAsWorkshopData(), // Nodes variable
                            map.AttributesAsWorkshopData(), // Attributes variable
                            baked // Bake variable
                        );
                    }
                });
            }).AsStruct(deltinScript);
        }
    }
}