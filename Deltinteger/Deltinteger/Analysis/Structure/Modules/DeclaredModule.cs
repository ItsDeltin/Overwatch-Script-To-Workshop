using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.Modules
{
    class DeclaredModule : ParentedDeclaredElement
    {
        readonly ScopeSource scopeSource;

        public DeclaredModule(ContextInfo contextInfo, IModuleContentProvider moduleContent)
        {
            Name = moduleContent.GetName();

            scopeSource = new ScopeSource();
            DeclaredElements = moduleContent.GetDeclarations(contextInfo.AddAppendableSource(scopeSource));
        }


        // Modules cannot be defined inside classes.
        public override void AddToContent(TypeContentBuilder contentBuilder) => throw new System.NotImplementedException();
    }
}