namespace DS.Analysis.Types
{
    using Generics;

    class CodeTypeProvider
    {
        public string Name { get; }
        public TypeArgCollection Generics { get; }

        public CodeTypeProvider(string name)
        {
            Name = name;
            Generics = TypeArgCollection.Empty;
        }

        public CodeTypeProvider(string name, TypeArgCollection typeArgCollection)
        {
            Name = name;
            Generics = typeArgCollection;
        }

        public CodeType CreateInstance(params CodeType[] typeArgs) => new CodeType(this);
    }
}