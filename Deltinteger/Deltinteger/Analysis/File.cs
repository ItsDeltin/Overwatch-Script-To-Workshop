using System.Linq;
using System.Collections.Generic;
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

        public FileParser FileParser { get; }

        BlockAction statements;


        public ScriptFile(string path, bool isExternal, DeltinScriptAnalysis analysis)
        {
            Path = path;
            IsExternal = isExternal;
            Analysis = analysis;
            Diagnostics = new FileDiagnostics(Path);
            FileParser = new FileParser(this);
        }


        public string GetRelativePath(string relativePath) => IOPath.GetFullPath(IOPath.Join(IOPath.GetDirectoryName(Path), relativePath));


        public void GetStructure()
        {
            // Get declarations
            RootScopeSource.Clear();
            statements?.Dispose();
            statements = new StructureContext(this, RootScopeSource).Block(FileParser.Syntax.Statements.ToArray(), RootScopeSource);

            GetMeta();
        }

        public void GetMeta() => statements.GetMeta(new ContextInfo(Analysis, this, Scope.Empty));

        public void GetContent() => statements.GetContent();


        public void Unlink()
        {
            statements?.Dispose();
            FileParser.Dispose();
        }
    }
}