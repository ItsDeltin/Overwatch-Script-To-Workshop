using DS.Analysis.Structure;

namespace DS.Analysis.Statements
{
    class DeclarationStatement : Statement
    {
        readonly AbstractDeclaredElement declaredElement;

        public DeclarationStatement(StructureContext structure, AbstractDeclaredElement declaredElement)
        {
            AddDisposable(this.declaredElement = declaredElement);
            structure.ScopeSource.AddScopedElement(declaredElement.MakeScopedElement());
        }

        public override void GetMeta(ContextInfo contextInfo) => declaredElement.GetMeta(contextInfo);
        public override void GetContent(ContextInfo contextInfo) => declaredElement.GetContent(contextInfo);
    }
}