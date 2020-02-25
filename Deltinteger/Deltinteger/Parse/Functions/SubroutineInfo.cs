using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SubroutineInfo
    {
        public Subroutine Subroutine { get; }
        public ReturnHandler ReturnHandler { get; }
        public TranslateRule Rule { get; }
        public IndexReference[] ParameterStores { get; }

        public SubroutineInfo(Subroutine routine, ReturnHandler returnHandler, TranslateRule rule, IndexReference[] parameterStores)
        {
            Subroutine = routine;
            ReturnHandler = returnHandler;
            Rule = rule;
            ParameterStores = parameterStores;
        }
    }
}