namespace DS.Analysis.Types
{
    using Components;

    class CodeType : IParentElement
    {
        public TypeLinker TypeLinker { get; }


        // Components
        public CodeTypeContent Content { get; set; }
        public ITypeComparison Comparison { get; set; }
        public IGetIdentifier GetIdentifier { get; set; }

        public CodeType()
        {
        }

        public CodeType(CodeType other)
        {
            Content = other.Content;
            Comparison = other.Comparison;
            GetIdentifier = other.GetIdentifier;
        }

        public override int GetHashCode() => Comparison.GetTypeHashCode();

        public static CodeType Create(CodeTypeContent content, ITypeComparison comparison, IGetIdentifier getIdentifier)
            => new CodeType() { Content = content, Comparison = comparison, GetIdentifier = getIdentifier };
    }
}