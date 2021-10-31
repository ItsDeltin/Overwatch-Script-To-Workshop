using DS.Analysis.Scopes;

namespace DS.Analysis.Types
{
    class CodeType
    {
        public CodeTypeProvider Provider { get; }
        public TypeLinker TypeLinker { get; }
        public Scope Scope { get; }

        public CodeType(CodeTypeProvider provider)
        {
            Provider = provider;
        }
    }
}