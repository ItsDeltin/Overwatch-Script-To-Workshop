using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class CompressedBakeComponent : IComponent, ISubroutineContext, IGroupDeterminer, IFunctionLookupTable
    {
        public DeltinScript DeltinScript { get; set; }

        public Element Result { get; private set; }

        private readonly ParameterHandler _bakeNodesParameter = new ParameterHandler("bakeNodes");
        private int _maxNodeCount;
        private SubroutineInfo _subroutineInfo;

        public void SetNodesValue(int maxNodesValue)
        {
            _maxNodeCount = maxNodesValue;
        }

        public void Init() {}

        public IParameterHandler[] Parameters() => new[] { _bakeNodesParameter };

        public SubroutineInfo GetSubroutineInfo()
        {
            if (_subroutineInfo == null)
            {
                var builder = new SubroutineBuilder(DeltinScript, this);
                builder.SetupSubroutine();
                _subroutineInfo = builder.SubroutineInfo;
            }
            return _subroutineInfo;
        }

        void ISubroutineContext.SetSubroutineInfo(SubroutineInfo subroutineInfo) => _subroutineInfo = subroutineInfo;
        void ISubroutineContext.Finish(Rule rule) { }
        string ISubroutineContext.RuleName() => "Pathfinder: Compressed Bake";
        string ISubroutineContext.ElementName() => "todo: pathfinder element name";
        string ISubroutineContext.ThisArrayName() => "todo: pathfinder array name";
        bool ISubroutineContext.VariableGlobalDefault() => true;
        IGroupDeterminer ISubroutineContext.GetDeterminer() => this;
        CodeType ISubroutineContext.ContainingType() => null;
        string IGroupDeterminer.GroupName() => "Pathfinder: Compressed Bake";
        bool IGroupDeterminer.IsRecursive() => false;
        bool IGroupDeterminer.IsObject() => false;
        bool IGroupDeterminer.IsSubroutine() => true;
        bool IGroupDeterminer.MultiplePaths() => false;
        bool IGroupDeterminer.IsVirtual() => false;
        bool IGroupDeterminer.ReturnsValue() => false;
        IFunctionLookupTable IGroupDeterminer.GetLookupTable() => this;
        RecursiveStack IGroupDeterminer.GetExistingRecursiveStack(List<RecursiveStack> stack) => throw new NotImplementedException();
        object IGroupDeterminer.GetStackIdentifier() => throw new NotImplementedException();

        void IFunctionLookupTable.Build(FunctionBuildController builder)
        {
            var actionSet = builder.ActionSet; // The action set.
            var matcher = GetMatcher(actionSet); // Get the character matcher.
            var nodeArray = _bakeNodesParameter.Index; // The index the node array is stored in.
            var nodeCount = Element.Part<V_CountOf>(nodeArray.Get()); // The number of nodes.
            var nodeResult = actionSet.VarCollection.Assign("compressBakeResult", true, false); // Assign the nodeResult.
            nodeResult.Set(actionSet, new V_EmptyArray()); // Initialize the nodeResult.

            // Loop through each node.
            var nodeArrayLoop = new ForBuilder(actionSet, "compressBakeNodeLoop", nodeCount);
            nodeArrayLoop.Init();

            nodeResult.Set(actionSet, new V_EmptyArray(), index: nodeArrayLoop.Value);

            var currentStringArray = nodeArray.Get()[nodeArrayLoop.Value]; // The current string array.

            // Loop through each string.
            var stringArrayLoop = new ForBuilder(actionSet, "compressBakeStringLoop", Element.Part<V_CountOf>(currentStringArray));
            stringArrayLoop.Init();

            var currentString = currentStringArray[stringArrayLoop.Value]; // The current string.

            // Loop through each character
            var characterLoop = new ForBuilder(actionSet, "compressCharacterLoop", Element.Part<V_StringLength>(currentString));
            characterLoop.Init();

            // Get and store the current character.
            var storeCharacter = actionSet.VarCollection.Assign("compressCharacter", true, false);
            storeCharacter.Set(actionSet, Element.Part<V_StringSlice>(currentString, characterLoop.Value, (Element)1));
            var character = storeCharacter.Get();

            actionSet.AddAction(nodeResult.ModifyVariable(Operation.AppendToArray, Element.Part<V_IndexOfArrayValue>(matcher, character), index: nodeArrayLoop.Value));

            characterLoop.End();

            actionSet.AddAction(A_Wait.MinimumWait);
            actionSet.AddAction(A_Wait.MinimumWait);

            stringArrayLoop.End();
            nodeArrayLoop.End();

            Result = nodeResult.Get();
        }

        Element GetMatcher(ActionSet actionSet)
        {
            // Create an array of strings with a single character.
            var matcherArray = new Element[_maxNodeCount + 1];
            for (int i = 0; i <= _maxNodeCount; i++)
                matcherArray[i] = new V_CustomString(CharFromInt(i));

            // Set matcher.
            var storeMatcher = actionSet.VarCollection.Assign("compressBakeMatcher", true, false);
            storeMatcher.Set(actionSet.DeltinScript.InitialGlobal.ActionSet, Element.CreateArray(matcherArray));

            return storeMatcher.Get();
        }
    
        public static Element Create(Pathmap map, int[] attributes)
        {
            var nodeArray = new Element[map.Nodes.Length];
            for (int i = 0; i < nodeArray.Length; i++)
            {
                int[] pathfindResult = Dijkstra(map, attributes, i);
                var compressed = CompressIntArray(pathfindResult);

                nodeArray[i] = Element.CreateArray(compressed.Select(s => new V_CustomString(s)).ToArray());
            }

            // Create the final node array.
            return Element.CreateArray(nodeArray);
        }

        private static int[] Dijkstra(Pathmap map, int[] attributes, int node)
        {
            int nodeCount = map.Nodes.Length;

            var unvisited = new List<int>();
            var prev = new int[map.Nodes.Length];
            var dist = new double[map.Nodes.Length];

            // Initialize unvisited, prev, and distances.
            for (int i = 0; i < dist.Length; i++)
            {
                dist[i] = double.PositiveInfinity;
                unvisited.Add(i);
            }

            dist[node] = 0;

            while (unvisited.Count > 0)
            {
                var current = unvisited.OrderBy(unvisited => dist[unvisited]).First();
                unvisited.Remove(current);

                var neighbors = map.Segments
                    // Get the list of segments that contain the current node.
                    .Where(segment => segment.Node1 == current || segment.Node2 == current)
                    // Select the opposite node in the segment to get the list of neighbors.
                    .Select(segment => segment.Node1 == current ? segment.Node2 : segment.Node1)
                    // Only use the unvisited neighbors.
                    .Where(node => unvisited.Contains(node));
                
                foreach (var neighbor in neighbors)
                {
                    var neighborAttributes = map.Attributes.Where(m => m.Node1 == neighbor && m.Node2 == current);

                    var newDist = dist[current] + map.Nodes[current].DistanceTo(map.Nodes[neighbor]);
                    if (newDist < dist[neighbor] && (neighborAttributes.Count() == 0 || neighborAttributes.Any(attribute => attributes.Contains(attribute.Attribute))))
                    {
                        dist[neighbor] = newDist;
                        prev[neighbor] = current + 1;
                    }
                }
            }

            return prev;
        }

        private static List<string> CompressIntArray(int[] values)
        {
            var strings = new List<string>();
            string current = "";
            bool addCurrentStringAfterLoop = false;

            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];

                // Do not accept negative numbers.
                if (value < 0) throw new Exception($"Index {i} in {nameof(values)} is less than 0.");

                string newStringValue = CharFromInt(value);

                // Append the new character to the compressed string.
                string newCurrent = current + newStringValue;

                // If the new string exceeds the workshop's maximum string byte count...
                if (WorkshopEncoding.GetByteCount(newCurrent) > 256)
                {
                    // Then add 'current' to the list of strings.
                    strings.Add(current);
                    current = newStringValue;
                    addCurrentStringAfterLoop = false;
                }
                else
                {
                    // newCurrent is under the maximum byte length, set current to it.
                    current = newCurrent;
                    addCurrentStringAfterLoop = true;
                }
            }

            if (addCurrentStringAfterLoop)
                strings.Add(current);
            
            return strings;
        }
    
        static Encoding WorkshopEncoding => Encoding.BigEndianUnicode;
        static string CharFromInt(int value) => WorkshopEncoding.GetString(BitConverter.GetBytes(value + 1))[0].ToString();
    }
}