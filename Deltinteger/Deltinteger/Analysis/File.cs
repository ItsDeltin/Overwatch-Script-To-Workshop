using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Structure;
using DS.Analysis.Structure.Utility;
using DS.Analysis.Scopes;

namespace DS.Analysis
{
    class File
    {
        readonly DeltinScriptAnalysis _analysis;
        RootContext _syntax;
        DeclarationContainer _rootDeclarations;

        // The root scope of the file.
        public ScopeSource RootScopeSource { get; } = new ScopeSource();

        readonly Scope _rootScope;

        public File(DeltinScriptAnalysis analysis)
        {
            _analysis = analysis;
            _rootScope = new Scope(RootScopeSource);
        }

        public void Set(RootContext syntax)
        {
            Unlink();
            _syntax = syntax;
        }

        public void GetStructure()
        {
            RootScopeSource.Clear();

            // Get declarations
            _rootDeclarations = new DeclarationContainer(StructureUtility.DeclarationsFromSyntax(_syntax));
            _rootDeclarations.GetStructure(new StructureInfo(RootScopeSource));
        }

        public void GetMeta()
        {
            _rootDeclarations.GetMeta(new ContextInfo(_analysis, this, _rootScope));
        }

        public void GetContent()
        {
            _rootDeclarations.GetContent(new ContextInfo(_analysis, this, _rootScope));
        }

        public void Unlink()
        {
            RootScopeSource.Clear();
            _rootDeclarations?.Dispose();
        }
    }
}