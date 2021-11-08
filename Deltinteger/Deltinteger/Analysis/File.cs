using DS.Analysis.Structure;
using DS.Analysis.Scopes;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
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

        public FileDiagnostics Diagnostics { get; }

        // The root scope of the file.
        public ScopeSource RootScopeSource { get; } = new ScopeSource();

        readonly Lexer lexer;

        RootContext syntax;
        BlockAction statements;


        public ScriptFile(string path, bool isExternal, DeltinScriptAnalysis analysis)
        {
            Path = path;
            IsExternal = isExternal;
            Analysis = analysis;
            Diagnostics = new FileDiagnostics(Path);

            lexer = new Lexer();
        }


        public string GetRelativePath(string relativePath) => IOPath.GetFullPath(IOPath.Join(IOPath.GetDirectoryName(Path), relativePath));


        public FileUpdater GetFileUpdater() => new FileUpdater(this);

        public void SetFromString(string content)
        {
            lexer.Init(new VersionInstance(content));
            Parse();
        }

        public void SetFromSyntax(RootContext syntax)
        {
            Unlink();
            this.syntax = syntax;
            GetStructure();
        }

        void Parse()
        {
            var parser = new Parser(lexer);
            var context = parser.Parse();
            SetFromSyntax(context);
        }


        public void GetStructure()
        {
            RootScopeSource.Clear();

            // Get declarations
            statements = new StructureContext(this, RootScopeSource).Block(syntax.Statements.ToArray(), RootScopeSource);

            GetMeta();
        }

        public void GetMeta() => statements.GetMeta(new ContextInfo(Analysis, this, Scope.Empty));

        public void GetContent() => statements.GetContent();


        public void Unlink()
        {
            RootScopeSource.Clear();
            statements?.Dispose();
        }


        /// <summary>Incremental script updates.</summary>
        public class FileUpdater
        {
            readonly ScriptFile file;
            public FileUpdater(ScriptFile file) => this.file = file;
            public void Update(UpdateRange change) => file.lexer.Update(file.lexer.Content.Update(change), change);
            public void ApplyUpdates() => file.Parse();
        }
    }
}