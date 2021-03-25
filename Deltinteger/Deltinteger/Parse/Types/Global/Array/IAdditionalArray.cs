namespace Deltin.Deltinteger.Parse
{
    public interface ITypeArrayHandler
    {
        void OverrideArray(ArrayType array);
        IGettableAssigner GetArrayAssigner(IVariable variable);
        ArrayFunctionHandler GetFunctionHandler();
    }

    class DefaultArrayHandler : ITypeArrayHandler
    {
        public IGettableAssigner GetArrayAssigner(IVariable variable) => new DataTypeAssigner((Var)variable);
        public ArrayFunctionHandler GetFunctionHandler() => new ArrayFunctionHandler();
        public void OverrideArray(ArrayType array) {}
    }
}