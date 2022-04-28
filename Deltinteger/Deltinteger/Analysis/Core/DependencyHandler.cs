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

        readonly Action noMoreDependencies;


        public DependencyHandler(DSAnalysis master, Action noMoreDependencies = null)
        {
            dependents = new DependencyList(noMoreDependencies);
            this.Master = master;
            this.noMoreDependencies = noMoreDependencies;
        }

        public DependencyNode CreateNode(UpdateAction updateAction, params IDependable[] dependOn)
        {
            var node = new DependencyNode(updateAction, Master);

            foreach (var d in dependOn)
                node.DependOn(d);

            AddDisposable(node);
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
        readonly IMaster master;
        readonly DisposableCollection disposables = new DisposableCollection();
        readonly SerializedDisposableCollection disposeOnUpdate = new SerializedDisposableCollection();

        bool disposed;

        public DependencyNode(UpdateAction updateAction, IMaster master)
        {
            (this.updateAction, this.master) = (updateAction, master);
            MarkAsStale();
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
        public void MarkAsStale()
        {
            ThrowIfDisposed();
            master.AddStaleObject(this);
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
            disposeOnUpdate.Dispose();
            updateAction();
        }
    }
}