using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Types.Internal;

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
                        var nodes = nodesVariable.Get(actionSet);
                        var players = methodCall.Get(0);
                        var destination = methodCall.Get(1);
                        var bake = bakeVariable.Get(actionSet);

                        var pathExecutorComponent = actionSet.DeltinScript.GetComponent<PathExecutorComponent>();
                        var pathExecutor = setup.Match(
                            isClass: () => pathExecutorComponent.ClassExcecutor(actionSet.CurrentObject),
                            isStruct: () => pathExecutorComponent.StructExecutor(actionSet.CurrentObject)
                        );

                        var args = new ExecutorArgs(actionSet, players, bake, nodes, destination);
                        pathExecutor.Pathfind(args);
                        return null;
                    }
                });

                setup.AddStaticMethod(new FuncMethodBuilder()
                {
                    Name = "LoadAll",
                    Documentation = "todo",
                    Parameters = new CodeParameter[] {
                        new PathmapFileParameter("pathmapFile", "The pathmap file.", types),
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
                            map.NodesAsWorkshopDataWithEncodedIndex(), // Nodes variable
                            map.AttributesAsWorkshopData(), // Attributes variable
                            baked // Bake variable
                        );
                    }
                });

                setup.AddStaticMethod(new FuncMethodBuilder()
                {
                    Name = "LoadNodes",
                    Documentation = "todo",
                    Parameters = new CodeParameter[] {
                        new PathmapFileParameter("pathmapFile", "The pathmap file.", types),
                    },
                    ReturnType = types.VectorArray(),
                    Action = (actionSet, methodCall) =>
                    {
                        // Get the pathmap.
                        var map = (Pathmap)methodCall.AdditionalParameterData[0];
                        return map.NodesAsWorkshopDataWithEncodedIndex();
                    }
                });

                setup.AddStaticMethod(new FuncMethodBuilder()
                {
                    Name = "LoadAttributes",
                    Documentation = "todo",
                    Parameters = new CodeParameter[] {
                        new PathmapFileParameter("pathmapFile", "The pathmap file.", types),
                    },
                    ReturnType = types.VectorArray(),
                    Action = (actionSet, methodCall) =>
                    {
                        // Get the pathmap.
                        var map = (Pathmap)methodCall.AdditionalParameterData[0];
                        return map.AttributesAsWorkshopData();
                    }
                });

                setup.AddStaticMethod(new FuncMethodBuilder()
                {
                    Name = "LoadBake",
                    Documentation = "todo",
                    Parameters = new CodeParameter[] {
                        new PathmapFileParameter("pathmapFile", "The pathmap file.", types),
                        new ConstIntegerArrayParameter("attributes", "todo", types, true),
                    },
                    ReturnType = types.Any(),
                    Action = (actionSet, methodCall) =>
                    {
                        // Get the pathmap.
                        var map = (Pathmap)methodCall.AdditionalParameterData[0];
                        var attributes = ((List<int>)methodCall.AdditionalParameterData[1]).ToArray();
                        return PathfindUtility.DecompressPathmap(actionSet, map, attributes);
                    }
                });
            }).AsStruct(deltinScript);
        }
    }
}