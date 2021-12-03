namespace DS.Analysis.Types
{
    using Components;

    class CodeType : IParentElement
    {
        public TypeLinker TypeLinker { get; }


        // Components
        public CodeTypeContent Content { get; protected set; }
        public ITypeComparison Comparison { get; protected set; }
        public IGetIdentifier GetIdentifier { get; protected set; }

        public CodeType()
        {
        }

        public override int GetHashCode() => Comparison.GetTypeHashCode();

        public static CodeType Create(CodeTypeContent content, ITypeComparison comparison, IGetIdentifier getIdentifier)
            => new CodeType() { Content = content, Comparison = comparison, GetIdentifier = getIdentifier };
    }
}