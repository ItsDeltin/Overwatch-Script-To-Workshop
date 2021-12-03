using DS.Analysis.Files;
using DS.Analysis.ModuleSystem;
using DS.Analysis.Scopes;

namespace DS.Analysis
{
    class DeltinScriptAnalysis
    {
        public FileManager FileManager { get; }
        public ModuleManager ModuleManager { get; } = new ModuleManager();
        public PostAnalysisOperation PostAnalysisOperations { get; } = new PostAnalysisOperation();

        public Scope DefaultScope { get; }

        public DeltinScriptAnalysis()
        {
            FileManager = new FileManager(this);
            DefaultScope = new Scope(Types.Standard.StandardTypes.StandardSource, ModuleManager.Root);
        }
    }
}