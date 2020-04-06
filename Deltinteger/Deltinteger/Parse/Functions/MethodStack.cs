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
}