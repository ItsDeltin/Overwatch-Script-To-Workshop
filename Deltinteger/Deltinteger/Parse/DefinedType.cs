namespace Deltin.Deltinteger.Parse
{
    public class DefinedType
    {
        public string Name { get; }
        public TypeKind DefineType { get; }
        public DefineNode[] DefinedVars { get; }
        public ConstructorNode[] Constructors { get; }
        public DefinedType(TypeDefineNode node)
        {
            Name = node.Name;
            DefineType = node.DefineType;
            DefinedVars = node.DefinedVars;
            Constructors = node.Constructors;
        }
    }

    public enum TypeKind
    {
        Class,
        Struct
    }

    public enum AccessLevel
    {
        Public,
        Private
    }
}