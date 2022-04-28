using System;
using System.Reactive.Disposables;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis
{
    using Core;
    using Utility;

    abstract class AnalysisObject : IDependable, IDependent, IUpdatable, IDisposable
    {
        /// <summary>If <see langword="true"/>, the object needs to be updated.</summary>
        public bool Stale { get; private set; }

        /// <summary>Determines if the object was disposed.</summary>
        public bool Disposed { get; private set; }

        protected IMaster Master { get; }

        /// <summary>The objects that depend on this.</summary>
        private readonly DependencyList dependents;

        private readonly DisposableCollection disposables = new DisposableCollection();

        private readonly SerializedDisposableCollection serializedDisposables = new SerializedDisposableCollection();


        protected AnalysisObject(IMaster master)
        {
            dependents = new DependencyList(NoMoreDependents);
            this.Master = master;
        }

        public virtual void Update()
        {
            serializedDisposables.Dispose();
            Stale = false;
            MarkDependentsAsStale();
        }

        public void MarkAsStale()
        {
            Stale = true;
            Master.AddStaleObject(this);
        }

        protected void MarkDependentsAsStale() => dependents.MarkAsStale();

        /// <summary>Adds a dependency to an IDependable.</summary>
        /// <param name="dependable">The IDependable to add a dependency to.</param>
        /// <param name="mode">Determines when the dependency is removed.
        /// <para>If <see cref="DependencyMode.DoNotManage"/>: it is
        /// expected that the caller manage the IDisposable.</para>
        /// <para>If <see cref="DependencyMode.RemoveOnFinalization"/>: the
        /// dependency is removed when this <see cref="AnalysisObject"/> is deleted.</para>
        /// <para>If <see cref="DependencyMode.RemoveOnUpdate"/>: the
        /// dependency is removed when <see cref="Update"/> is called.</para>
        /// </param>
        /// <returns>An IDisposable which is used to remove the dependency if <paramref name="mode"/>
        /// is <see cref="DependencyMode.DoNotManage"/>.</returns>
        protected IDisposable DependOn(IDependable dependable, DependencyMode mode = DependencyMode.RemoveOnFinalization)
        {
            var unlinker = dependable.AddDependent(this);
            MarkAsStale();

            if (mode != DependencyMode.DoNotManage)
            {
                AddDisposable(unlinker, disposeOnUpdate: mode == DependencyMode.RemoveOnUpdate);
                return Disposable.Empty;
            }

            return unlinker;
        }

        public IDisposable AddDependent(IDependent dependent)
        {
            ThrowIfDisposed();
            return dependents.Add(dependent);
        }

        protected virtual void NoMoreDependents() { }

        protected T DependOnAndHost<T>(T analysisObject) where T : IDisposable, IDependable
        {
            DependOn(analysisObject);
            AddDisposable(analysisObject);
            return analysisObject;
        }

        protected T AddDisposable<T>(T disposable, bool disposeOnUpdate = false) where T : IDisposable
        {
            // Will be disposed when the Update method is called.
            if (disposeOnUpdate)
                serializedDisposables.Add(disposable);
            // Will be disposed when the AnalysisObject is disposed.
            else
                disposables.Add(disposable);

            return disposable;
        }

        public virtual void Dispose()
        {
            ThrowIfDisposed();

            Disposed = true;
            disposables.Dispose();
            serializedDisposables.Dispose();
        }

        protected void ThrowIfDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(ToString());
        }

        protected enum DependencyMode
        {
            DoNotManage,
            RemoveOnFinalization,
            RemoveOnUpdate
        }
    }
}