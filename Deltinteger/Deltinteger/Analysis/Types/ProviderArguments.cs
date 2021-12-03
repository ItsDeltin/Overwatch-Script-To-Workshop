namespace DS.Analysis.Types
{
    class ProviderArguments
    {
        public static readonly ProviderArguments Default = new ProviderArguments();


        public CodeType[] TypeArgs { get; }
        public IParentElement Parent { get; }


        public ProviderArguments()
        {
            TypeArgs = new CodeType[0];
        }

        public ProviderArguments(CodeType[] typeArgs, IParentElement parent)
        {
            TypeArgs = typeArgs;
            Parent = parent;
        }
    }
}