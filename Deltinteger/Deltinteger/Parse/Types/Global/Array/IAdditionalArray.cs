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
        public virtual IGettableAssigner GetArrayAssigner(AssigningAttributes attributes) => new DataTypeAssigner(attributes);
        public virtual ArrayFunctionHandler GetFunctionHandler() => new ArrayFunctionHandler();
        public virtual void OverrideArray(ArrayType array) { }
    }
}