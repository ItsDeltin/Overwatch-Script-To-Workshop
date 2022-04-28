using System;
using System.Collections.Generic;

namespace DS.Analysis
{
    using Files;
    using ModuleSystem;
    using Scopes;
    using Core;

    class DSAnalysis : IMaster
    {
        public FileManager FileManager { get; }
        public ModuleManager ModuleManager { get; }
        public PostAnalysisOperation PostAnalysisOperations { get; } = new PostAnalysisOperation();

        public Scope DefaultScope { get; }

        /// <summary>The objects that need to be updated.</summary>
        readonly List<IUpdatable> staleObjects = new List<IUpdatable>();

        public DSAnalysis()
        {
            FileManager = new FileManager(this);
            ModuleManager = new ModuleManager(this);
            DefaultScope = new Scope(this, Types.StandardType.StandardSource, ModuleManager.Root);
        }

        public void Update()
        {
            while (staleObjects.Count > 0)
            {
                staleObjects[0].Update();
                staleObjects.RemoveAt(0);
            }
        }

        public void AddStaleObject(IUpdatable analysisObject)
        {
            if (!staleObjects.Contains(analysisObject))
                staleObjects.Add(analysisObject);
        }

        public void RemoveObject(IUpdatable updatable)
        {
            staleObjects.Remove(updatable);
        }


        // Creates a DependencyHandler with a node.
        public SingleNode SingleNode(Action updateAction) => new SingleNode(new DependencyHandler(this), updateAction);
    }
}