using DS.Analysis.Scopes;

namespace DS.Analysis.Types
{
    using Components;

    class CodeType
    {
        public TypeLinker TypeLinker { get; }
        public Scope Scope { get; }

        public IAssignableTo AssignableTo { get; protected set; }

        public CodeType()
        {
        }
    }
}