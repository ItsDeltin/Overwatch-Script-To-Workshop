namespace DS.Analysis.Types
{
    using Components;

    class CodeType
    {
        public TypeLinker TypeLinker { get; }


        // Components
        public CodeTypeContent Content { get; protected set; }
        public IAssignableTo AssignableTo { get; protected set; }

        public CodeType()
        {
        }

        public static CodeType Create(CodeTypeContent content) => new CodeType() { Content = content };
    }
}