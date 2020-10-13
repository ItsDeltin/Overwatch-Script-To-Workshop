using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.FunctionBuilder;

namespace Deltin.Deltinteger.Parse
{
    public class SubroutineInfo
    {
        public Subroutine Subroutine { get; }
        public FunctionBuildController FunctionBuilder { get; }
        public IndexReference[] ParameterStores { get; }
        public IndexReference ObjectStore { get; }
        public ReturnHandler ReturnHandler => FunctionBuilder.ReturnHandler;

        public SubroutineInfo(Subroutine routine, FunctionBuildController functionBuilder, IndexReference[] parameterStores, IndexReference objectStore)
        {
            Subroutine = routine;
            FunctionBuilder = functionBuilder;
            ParameterStores = parameterStores;
            ObjectStore = objectStore;
        }
    }
}