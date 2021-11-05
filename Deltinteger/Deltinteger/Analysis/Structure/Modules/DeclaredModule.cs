using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.Modules
{
    class DeclaredModule : ParentedDeclaredElement
    {
        readonly ScopeSource scopeSource;

        public DeclaredModule(StructureContext structure, IModuleContentProvider moduleContent)
        {
            Name = moduleContent.GetName();
            
            scopeSource = new ScopeSource();
            DeclaredElements = moduleContent.GetDeclarations(structure.SetScopeSource(scopeSource));
        }
    }
}