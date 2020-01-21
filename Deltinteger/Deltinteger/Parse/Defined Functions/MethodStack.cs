namespace Deltin.Deltinteger.Parse
{
    public class MethodStack
    {
        public IApplyBlock Function { get; }

        public MethodStack(IApplyBlock function)
        {
            Function = function;
        }
    }

    public class RecursiveMethodStack : MethodStack
    {
        public ReturnHandler ReturnHandler { get; }
        public IndexReference ContinueSkipArray { get; }
        public SkipEndMarker MethodStart { get; }

        public RecursiveMethodStack(DefinedMethod method, ReturnHandler returnHandler, IndexReference continueSkipArray, SkipEndMarker methodStart) : base(method)
        {
            ReturnHandler = returnHandler;
            ContinueSkipArray = continueSkipArray;
            MethodStart = methodStart;
        }
    }
}