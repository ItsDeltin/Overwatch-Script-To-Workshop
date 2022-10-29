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
            PathResolve = new PathResolveClass(deltinScript);
            Bakemap = new BakemapClass(deltinScript);

            deltinScript.Types.AddType(Pathmap.Provider);
            deltinScript.Types.AddType(PathResolve.Provider);
            deltinScript.Types.AddType(Bakemap.Provider);
            deltinScript.Types.AddType(BakemapStruct.Create(deltinScript));
        }
    }
}