namespace Deltin.Deltinteger.Parse
{
    public interface ITypeArrayHandler
    {
        void OverrideArray(ArrayType array);
        IGettableAssigner GetArrayAssigner(AssigningAttributes attributes);
        ArrayFunctionHandler GetFunctionHandler();
    }

    class DefaultArrayHandler : ITypeArrayHandler
    {
        public IGettableAssigner GetArrayAssigner(AssigningAttributes attributes) => new DataTypeAssigner(attributes);
        public ArrayFunctionHandler GetFunctionHandler() => new ArrayFunctionHandler();
        public void OverrideArray(ArrayType array) {}
    }
}