using System;

namespace DS.Analysis
{
    using Core;

    /// <summary>
    /// Convenient access to a DependencyHandler with a single node.
    /// </summary>
    class SingleNode : IDisposable
    {
        public bool HasDependents => dependencyHandler.HasDependents;
        public DSAnalysis Master => dependencyHandler.Master;

        readonly DependencyHandler dependencyHandler;
        readonly DependencyNode node;

        public SingleNode(DependencyHandler dependencyHandler, Action action)
        {
            this.dependencyHandler = dependencyHandler;
            node = dependencyHandler.CreateNode(action.Invoke);
        }

        public void MakeDependentsStale() => dependencyHandler.MakeDependentsStale();
        public IDisposable AddDependent(IDependent dependent) => dependencyHandler.AddDependent(dependent);
        public IDisposable AddDisposable(IDisposable disposable) => dependencyHandler.AddDisposable(disposable);

        public IDisposable DependOn(IDependable dependable) => node.DependOn(dependable);
        public void DisposeOnUpdate(IDisposable disposable) => node.DisposeOnUpdate(disposable);


        public void Dispose() => dependencyHandler.Dispose();
    }
}