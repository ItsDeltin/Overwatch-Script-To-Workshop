using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Pathfinder
{
    class PathfindUtility
    {
        public static Element GetBakemapTargetNode(Element nodeArray, Element destination)
        {
            return IndexOfArrayValue(
                nodeArray,
                FirstOf(Sort(
                    // Sort non-null nodes
                    /*Element.Part<V_FilteredArray>(nodeArray, new V_ArrayElement()),*/
                    nodeArray,
                    // Sort by distance to destination
                    DistanceBetween(ArrayElement(), destination)
                ))
            );
        }

        public static Element CompressedBakemapFromPathmapAndAttributes(Pathmap pathmap, int[] attributes)
            => Cache.CacheWatcher.Global.Get<Element>(new CompressedBakeCacheObject(pathmap, attributes));

        public static Element DecompressPathmap(ActionSet actionSet, Pathmap pathmap, int[] attributes)
        {
            // Get the compressed bakemap data.
            var compressed = CompressedBakemapFromPathmapAndAttributes(pathmap, attributes);

            // Get the CompressedBakeComponent.
            var component = actionSet.DeltinScript.GetComponent<CompressedBakeComponent>();

            // Call the decompresser.
            return component.Build(actionSet, compressed, null, null);
        }
    }
}