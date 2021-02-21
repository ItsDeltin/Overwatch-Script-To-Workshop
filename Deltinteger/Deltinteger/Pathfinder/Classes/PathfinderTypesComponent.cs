using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathfinderTypesComponent : IComponent
    {
        public PathmapClass Pathmap { get; private set; }
        public PathResolveClass PathResolve { get; private set; }
        public BakemapClass Bakemap { get; private set; }

        public void Init(DeltinScript deltinScript)
        {
            Pathmap = new PathmapClass(deltinScript);
            PathResolve = new PathResolveClass(deltinScript.Types);
            Bakemap = new BakemapClass(deltinScript.Types);

            deltinScript.Types.AddType(Pathmap.Instance);
            deltinScript.Types.AddType(PathResolve.Instance);
            deltinScript.Types.AddType(Bakemap.Instance);
        }
    }
}