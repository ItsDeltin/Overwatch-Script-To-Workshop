namespace DS.Analysis.Types
{
    class ProviderArguments
    {
        public CodeType[] TypeArgs { get; }
        public IParentElement Parent { get; }

        public ProviderArguments(CodeType[] typeArgs, IParentElement parent)
        {
            TypeArgs = typeArgs;
            Parent = parent;
        }
    }
}