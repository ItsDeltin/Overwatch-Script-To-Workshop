namespace Deltin.Deltinteger.Parse
{
    interface IAdditionalArray
    {
        void OverrideArray(ArrayType array);
        IGettableAssigner GetArrayAssigner(IVariable variable);
        ArrayFunctionHandler GetFunctionHandler();
    }
}