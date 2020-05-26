using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SubroutineInfo
    {
        public Subroutine Subroutine { get; }
        public ReturnHandler ReturnHandler { get; }
        public TranslateRule Rule { get; }
        public IndexReference[] ParameterStores { get; }
        public IndexReference ObjectStore { get; }

        public SubroutineInfo(Subroutine routine, ReturnHandler returnHandler, IndexReference[] parameterStores, IndexReference objectStore)
        {
            Subroutine = routine;
            ReturnHandler = returnHandler;
            ParameterStores = parameterStores;
            ObjectStore = objectStore;
        }
    }
}