using DS.Analysis.Files;

namespace DS.Analysis
{
    class DeltinScriptAnalysis
    {
        public FileManager FileManager { get; }

        public DeltinScriptAnalysis()
        {
            FileManager = new FileManager(this);
        }
    }
}