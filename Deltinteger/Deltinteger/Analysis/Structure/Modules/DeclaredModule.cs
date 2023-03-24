using System;
using System.Linq;
using DS.Analysis.Scopes;
using DS.Analysis.ModuleSystem;

namespace DS.Analysis.Structure.Modules
{
    class DeclaredModule : ParentedDeclaredElement, IModuleSource
    {
        readonly ScopeSource scopeSource;
        readonly IDisposable moduleSourceReference;
        readonly IGetIdentifier identifier;


        public DeclaredModule(ContextInfo contextInfo, IModuleContentProvider moduleContent)
        {
            Name = moduleContent.GetName();

            var path = contextInfo.ModulePath.Append(Name);

            // Create the module scope.
            scopeSource = new ScopeSource();
            DeclaredElements = moduleContent.GetDeclarations(contextInfo.SetModulePath(path).AddAppendableSource(scopeSource));

            // Add the source to the module manager.
            moduleSourceReference = contextInfo.Analysis.ModuleManager.AddModuleSource(scopeSource, path.ToArray());

            identifier = contextInfo.CreateStructuredIdentifier(Name, element => element.TypePartHandler == this);
        }

        public override void AddToScope(IScopeAppender scopeAppender)
        {
        }

        // Modules cannot be defined inside classes.
        public override void AddToContent(TypeContentBuilder contentBuilder) => throw new System.NotImplementedException();


        public override void Dispose()
        {
            base.Dispose();
            moduleSourceReference.Dispose();
        }
    }
}