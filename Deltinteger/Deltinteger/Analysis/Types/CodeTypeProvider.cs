namespace DS.Analysis.Types
{
    class CodeTypeProvider
    {
        public string Name { get; }

        public CodeTypeProvider(string name)
        {
            Name = name;
        }

        public virtual bool IsMatch(string typeName) => typeName == Name;

        public CodeType CreateInstance(CodeType[] typeArgs) => new CodeType(this);
    }
}