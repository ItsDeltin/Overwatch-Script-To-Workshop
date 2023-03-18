using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DS.Analysis
{
    using Files;
    using ModuleSystem;
    using Scopes;
    using Core;

    class DSAnalysis
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

        public void AddStaleObject(IUpdatable analysisObject, StaleObject debug)
        {
            if (!staleObjects.Contains(analysisObject))
            {
                staleObjects.Add(analysisObject);
                Debug.WriteLine("[+] " + debug);
            }
            else
                Debug.WriteLine("[ ] " + debug);
        }

        public void RemoveObject(IUpdatable updatable)
        {
            staleObjects.Remove(updatable);
        }


        // Creates a DependencyHandler with a node.
        public SingleNode SingleNode(string name, Action updateAction) => new SingleNode(new DependencyHandler(this, name), updateAction, name);

        public DependencyNode OnlyNode(string name, Action updateAction) => new DependencyNode(updateAction.Invoke, this, name);
    }

    readonly struct StaleObject
    {
        public readonly string Name;
        public readonly string? Source;

        public StaleObject(string name, string? source)
        {
            Name = name;
            Source = source;
        }

        public StaleObject(string name)
        {
            Name = name;
            Source = null;
        }

        public override string ToString()
        {
            if (Source == null)
                return $"\"{Name}\" INIT";
            else
                return $"\"{Name}\" < \"{Source}\"";
        }
    }
}