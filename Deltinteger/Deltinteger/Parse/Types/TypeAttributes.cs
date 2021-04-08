namespace Deltin.Deltinteger.Parse
{
    public class TypeAttributes
    {
        public bool IsStruct { get; set; }
        public bool ContainsGenerics { get; set; }

        public TypeAttributes() {}

        public TypeAttributes(bool isStruct, bool containsGenerics)
        {
            IsStruct = isStruct;
            ContainsGenerics = containsGenerics;
        }
    }
}