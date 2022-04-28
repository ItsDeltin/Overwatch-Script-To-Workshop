using System.Collections.Generic;

namespace DS.Analysis
{
    using Files;
    using ModuleSystem;
    using Scopes;
    using Core;

    class DeltinScriptAnalysis : IMaster
    {
        public FileManager FileManager { get; }
        public ModuleManager ModuleManager { get; }
        public PostAnalysisOperation PostAnalysisOperations { get; } = new PostAnalysisOperation();

        public Scope DefaultScope { get; }

        /// <summary>The objects that need to be updated.</summary>
        readonly Queue<IUpdatable> staleObjects = new Queue<IUpdatable>();

        public DeltinScriptAnalysis()
        {
            FileManager = new FileManager(this);
            ModuleManager = new ModuleManager(this);
            DefaultScope = new Scope(this, Types.StandardType.StandardSource, ModuleManager.Root);
        }

        public void Update()
        {
            while (staleObjects.TryDequeue(out var nextStaleObject))
                nextStaleObject.Update();
        }

        public void AddStaleObject(IUpdatable analysisObject)
        {
            if (!staleObjects.TryPeek(out var currentStaleObject) || currentStaleObject != analysisObject)
                staleObjects.Enqueue(analysisObject);
        }
    }
}