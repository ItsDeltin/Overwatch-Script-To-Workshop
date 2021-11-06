using DS.Analysis.Structure;
using DS.Analysis.Scopes;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler.Parse;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using IOPath = System.IO.Path;

namespace DS.Analysis
{
    class ScriptFile
    {
        public string Path { get; }
        
        /// <summary>If the file is external, it will be unloaded when there are no more dependencies.</summary>
        public bool IsExternal { get; }
        
        public DeltinScriptAnalysis Analysis { get; }

        public FileDiagnostics Diagnostics { get; } = new FileDiagnostics();

        RootContext syntax;
        BlockAction statements;

        // The root scope of the file.
        public ScopeSource RootScopeSource { get; } = new ScopeSource();


        public ScriptFile(string path, bool isExternal, DeltinScriptAnalysis analysis)
        {
            Path = path;
            IsExternal = isExternal;
            Analysis = analysis;
        }


        public string GetRelativePath(string relativePath) => IOPath.GetFullPath(IOPath.Join(IOPath.GetDirectoryName(Path), relativePath));


        public void SetFromString(string content)
        {
            var lex = new Lexer();
            var par = new Parser(lex);

            lex.Init(new VersionInstance(content));
            var context = par.Parse();

            SetFromSyntax(context);
        }

        public void SetFromSyntax(RootContext syntax)
        {
            Unlink();
            this.syntax = syntax;
        }


        public void GetStructure()
        {
            RootScopeSource.Clear();

            // Get declarations
            statements = new StructureContext(this, RootScopeSource).Block(syntax.Statements.ToArray(), RootScopeSource);
        }

        public void GetMeta()
        {
            statements.GetMeta(new ContextInfo(Analysis, this, Scope.Empty));
        }

        public void GetContent()
        {
            statements.GetContent();
        }


        public void Unlink()
        {
            RootScopeSource.Clear();
            statements?.Dispose();
        }
    }
}