using DS.Analysis.Structure;
using DS.Analysis.Scopes;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using IOPath = System.IO.Path;

namespace DS.Analysis
{
    class File
    {
        public string Path { get; }
        public DeltinScriptAnalysis Analysis { get; }

        RootContext syntax;
        BlockAction statements;

        // The root scope of the file.
        public ScopeSource RootScopeSource { get; } = new ScopeSource();

        readonly Scope rootScope;

        public File(DeltinScriptAnalysis analysis)
        {
            this.Analysis = analysis;
            rootScope = new Scope(RootScopeSource);
        }


        public string GetRelativePath(string relativePath) => IOPath.GetFullPath(IOPath.Join(Path, relativePath));


        public void Set(RootContext syntax)
        {
            Unlink();
            this.syntax = syntax;
        }

        public void GetStructure()
        {
            RootScopeSource.Clear();

            // Get declarations
            statements = new StructureContext(this, RootScopeSource).Block(syntax.Statements.ToArray());
        }

        public void GetMeta()
        {
            statements.GetMeta(new ContextInfo(Analysis, this, rootScope));
        }

        public void GetContent()
        {
            statements.GetContent(new ContextInfo(Analysis, this, rootScope));
        }

        public void Unlink()
        {
            RootScopeSource.Clear();
            statements?.Dispose();
        }
    }
}