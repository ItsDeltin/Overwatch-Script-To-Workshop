using DS.Analysis.Files;
using DS.Analysis.ModuleSystem;

namespace DS.Analysis
{
    class DeltinScriptAnalysis
    {
        public FileManager FileManager { get; }
        public ModuleManager ModuleManager { get; } = new ModuleManager();

        public DeltinScriptAnalysis()
        {
            FileManager = new FileManager(this);
        }
    }
}