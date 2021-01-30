using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class CompressedBakeComponent : IComponent
    {
        public DeltinScript DeltinScript { get; set; }

        public Element Result { get; private set; }

        private int _maxNodeCount;
        private Element _matcher;

        public void SetNodesValue(int maxNodesValue)
        {
            _maxNodeCount = Math.Max(_maxNodeCount, maxNodesValue);
        }

        public void Init() {}

        public void Build(ActionSet actionSet, Element compressedNodeArray, Action<Element> printProgress, ILambdaInvocable onLoop)
        {
            var matcher = GetMatcher(actionSet); // Get the character matcher.
            var nodeArray = actionSet.VarCollection.Assign("compressedNodes", actionSet.IsGlobal, false); // The index the node array is stored in.
            var nodeCount = Element.CountOf(nodeArray.Get()); // The number of nodes.
            var bakeResult = actionSet.VarCollection.Assign("compressBakeResult", true, false); // Assign the nodeResult.
            var compressCurrentNodeArray = actionSet.VarCollection.Assign("compressCurrentNodeArray", true, false); // Assign the nodeResult.

            nodeArray.Set(actionSet, compressedNodeArray);
            bakeResult.Set(actionSet, Element.EmptyArray()); // Initialize the nodeResult.

            // Loop through each node.
            var nodeArrayLoop = new ForBuilder(actionSet, "compressBakeNodeLoop", nodeCount);
            printProgress(nodeArrayLoop.Value / nodeCount); // Print the node count.
            nodeArrayLoop.Init();

            compressCurrentNodeArray.Set(actionSet, Element.EmptyArray());

            var currentStringArray = nodeArray.Get()[nodeArrayLoop.Value]; // The current string array.

            // Loop through each string.
            var stringArrayLoop = new ForBuilder(actionSet, "compressBakeStringLoop", Element.CountOf(currentStringArray));
            stringArrayLoop.Init();

            var currentString = currentStringArray[stringArrayLoop.Value]; // The current string.

            // Create an array with the length of the number of characters in the string.
            var mapper = actionSet.VarCollection.Assign("compressMapper", actionSet.IsGlobal, false);
            mapper.Set(actionSet, Element.EmptyArray());
            mapper.Set(actionSet, index: Element.StringLength(currentString) - 1, value: 0);

            actionSet.AddAction(compressCurrentNodeArray.ModifyVariable(
                operation: Operation.AppendToArray,
                value: Element.Map(
                    mapper.Get(),
                    Element.IndexOfArrayValue(
                        matcher,
                        Element.StringSlice(currentString, Element.ArrayIndex(), (Element)1)
                    )
                )
            ));

            // Invoke onLoop.
            if (onLoop == null)
                actionSet.AddAction(Element.Wait());
            else
                onLoop.Invoke(actionSet);

            stringArrayLoop.End();
            actionSet.AddAction(bakeResult.SetVariable(index: nodeArrayLoop.Value, value: compressCurrentNodeArray.Get()));
            nodeArrayLoop.End();
            Result = bakeResult.Get();
        }

        Element GetMatcher(ActionSet actionSet)
        {
            if (_matcher == null)
            {
                // Create an array of strings with a single character.
                var matcherArray = new Element[_maxNodeCount + 1];
                for (int i = 0; i <= _maxNodeCount; i++)
                    matcherArray[i] = Element.CustomString(CharFromInt(i).ToString());

                // Set matcher.
                var storeMatcher = actionSet.VarCollection.Assign("compressBakeMatcher", true, false);
                storeMatcher.Set(actionSet.DeltinScript.InitialGlobal.ActionSet, Element.CreateArray(matcherArray));
                _matcher = storeMatcher.Get();
            }

            return _matcher;
        }
    
        public static Element Create(Pathmap map, int[] attributes)
        {
            var nodeArray = new Element[map.Nodes.Length];
            for (int i = 0; i < nodeArray.Length; i++)
            {
                int[] pathfindResult = Dijkstra(map, attributes, i);
                var compressed = CompressIntArray(pathfindResult);

                nodeArray[i] = Element.CreateArray(compressed.Select(s => Element.CustomString(s)).ToArray());
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

                char newStringValue = CharFromInt(value);

                // Append the new character to the compressed string.
                string newCurrent = current + newStringValue;

                // If the new string exceeds the workshop's maximum string byte count...
                if (WorkshopEncoding.GetByteCount(newCurrent) > 256)
                {
                    // Then add 'current' to the list of strings.
                    strings.Add(current);
                    current = newStringValue.ToString();
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
    
        static Encoding WorkshopEncoding => Encoding.Unicode;
        static char CharFromInt(int value)
        {
            var illegal = new[] { '\"', '\n', '\r', '\\', '{' };

            for (int i = 0, c = 0;; i++)
            {
                char r = WorkshopEncoding.GetString(BitConverter.GetBytes(i + 1))[0]; 

                if (illegal.Contains(r)) continue;
                if (value == c) return r;

                c++;
            }
        }
    }
}