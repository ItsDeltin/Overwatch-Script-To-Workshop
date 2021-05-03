namespace Deltin.Deltinteger.Parse
{
    public class TypeAttributes
    {
        public bool IsStruct { get; set; }
        public bool ContainsGenerics { get; set; }
        public int StackLength { get; set; } = 1;

        public TypeAttributes() {}

        public TypeAttributes(bool isStruct, bool containsGenerics)
        {
            IsStruct = isStruct;
            ContainsGenerics = containsGenerics;
        }
    }
}