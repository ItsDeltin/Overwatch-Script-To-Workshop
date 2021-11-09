namespace DS.Analysis.Types
{
    class CodeTypeProvider
    {
        public string Name { get; }

        public CodeTypeProvider(string name)
        {
            Name = name;
        }

        public CodeType CreateInstance(params CodeType[] typeArgs) => new CodeType(this);
    }
}