using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Diagnostics;

namespace DS.Analysis.Core
{
    using Utility;

    delegate void UpdateAction();

    sealed class DependencyHandler : IDisposable, IDependable
    {
        public DSAnalysis Master { get; }

        public bool HasDependents => dependents.HasDependents;

        readonly DependencyList dependents;
        readonly DisposableCollection disposables = new DisposableCollection();


        public DependencyHandler(DSAnalysis master, string name)
        {
            dependents = new DependencyList(name);
            this.Master = master;
        }

        public DependencyNode CreateNode(UpdateAction updateAction, string name, params IDependable[] dependOn)
        {
            var node = new DependencyNode(updateAction, Master, name);

            foreach (var d in dependOn)
                node.DependOn(d);

            AddDisposable(node);
            return node;
        }

        public DependencyNode CreateNode(UpdateAction updateAction, string name, out IDisposable removeNode)
        {
            var node = new DependencyNode(updateAction, Master, name);
            removeNode = AddDisposable(node);
            return node;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public void MakeDependentsStale() => dependents.MarkAsStale();

        public IDisposable AddDisposable(IDisposable disposable) => disposables.Add(disposable);

        public void AddDisposables(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
                AddDisposable(disposable);
        }

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => dependents.Add(dependent);

        // IDisposable
        public void Dispose() => disposables.Dispose();
    }

    class DependencyNode : IDisposable, IDependent, IUpdatable
    {
        readonly UpdateAction updateAction;
        readonly DSAnalysis master;
        readonly string name;

        readonly DisposableCollection disposables = new DisposableCollection();
        readonly SerializedDisposableCollection disposeOnUpdate = new SerializedDisposableCollection();

        bool stale;
        bool disposed;

        public DependencyNode(UpdateAction updateAction, DSAnalysis master, string name)
        {
            (this.updateAction, this.master, this.name) = (updateAction, master, name);
            MarkAsStale(null);
        }

        public IDisposable DependOn(IDependable dependable)
        {
            ThrowIfDisposed();
            return disposables.Add(dependable.AddDependent(this));
        }

        public void DisposeOnUpdate(IDisposable disposable)
        {
            ThrowIfDisposed();
            disposeOnUpdate.Add(disposable);
        }

        // IDependent
        public void MarkAsStale(string source)
        {
            ThrowIfDisposed();
            stale = true;
            master.AddStaleObject(this, new StaleObject(name, source));
        }

        // IDisposable
        public void Dispose()
        {
            ThrowIfDisposed();
            disposed = true;
            disposables.Dispose();
            disposeOnUpdate.Dispose();
            master.RemoveObject(this);
        }

        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(ToString());
        }

        // IUpdatable
        [DebuggerStepThrough]
        public void Update()
        {
            ThrowIfDisposed();
            stale = false;
            disposeOnUpdate.Dispose();
            updateAction();
        }
    }
}