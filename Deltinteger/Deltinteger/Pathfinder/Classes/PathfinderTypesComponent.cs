using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathfinderTypesComponent : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        public PathmapClass Pathmap { get; private set; }
        public PathResolveClass PathResolve { get; private set; }
        public BakemapClass Bakemap { get; private set; }

        public void Init()
        {
            Pathmap = new PathmapClass(DeltinScript);
            PathResolve = new PathResolveClass(DeltinScript.Types);
            Bakemap = new BakemapClass(DeltinScript.Types);

            DeltinScript.Types.AddType(Pathmap.Instance);
            DeltinScript.Types.AddType(PathResolve.Instance);
            DeltinScript.Types.AddType(Bakemap.Instance);
        }
    }
}