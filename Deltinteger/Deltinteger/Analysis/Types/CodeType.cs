namespace DS.Analysis.Types
{
    using Components;

    class CodeType
    {
        public TypeLinker TypeLinker { get; }


        // Components
        public CodeTypeContent Content { get; protected set; }
        public ITypeComparison Comparison { get; protected set; }

        public CodeType()
        {
        }

        public override int GetHashCode() => Comparison.GetTypeHashCode();

        public static CodeType Create(CodeTypeContent content, ITypeComparison comparison) => new CodeType() { Content = content, Comparison = comparison };
    }
}