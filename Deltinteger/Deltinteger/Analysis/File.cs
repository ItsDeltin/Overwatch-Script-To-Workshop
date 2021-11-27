using System;
using DS.Analysis.Scopes;
using DS.Analysis.Diagnostics;
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

        IDisposable postAnalysisDisposable;


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
            postAnalysisDisposable?.Dispose();
            statements = new ContextInfo(Analysis, this, Scope.Default).Block(FileParser.Syntax.Statements.ToArray(), RootScopeSource);

            postAnalysisDisposable = Analysis.PostAnalysisOperations.ExecuteAndReset();
        }


        public void Unlink()
        {
            statements?.Dispose();
            postAnalysisDisposable?.Dispose();
            FileParser.Dispose();
        }
    }
}