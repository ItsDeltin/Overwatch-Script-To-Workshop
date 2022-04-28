using System;
using DS.Analysis.Scopes;
using DS.Analysis.Diagnostics;
using IOPath = System.IO.Path;

namespace DS.Analysis
{
    using Utility;

    class ScriptFile
    {
        public string Path { get; }

        /// <summary>If the file is external, it will be unloaded when there are no more dependencies.</summary>
        public bool IsExternal { get; }

        public DSAnalysis Analysis { get; }

        public FileDiagnostics Diagnostics { get; }

        // The root scope of the file.
        public ScopeSource RootScopeSource { get; } = new ScopeSource();

        public FileParser FileParser { get; }

        readonly SerializedDisposableCollection disposables = new SerializedDisposableCollection();

        BlockAction statements;

        IDisposable postAnalysisDisposable;


        public ScriptFile(string path, bool isExternal, DSAnalysis analysis)
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
            // Reset declarations
            RootScopeSource.Clear();
            statements?.Dispose();
            postAnalysisDisposable?.Dispose();
            disposables.Dispose();

            // Get the statements
            statements = new ContextInfo(Analysis, this, Analysis.DefaultScope, disposables).Block(FileParser.Syntax.Statements.ToArray(), RootScopeSource);

            Analysis.Update();

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