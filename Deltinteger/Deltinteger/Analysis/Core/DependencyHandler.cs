using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Core
{
    using Utility;

    sealed class DependencyHandler : IDisposable, IDependable, IDependent
    {
        public IMaster Master { get; }

        public bool HasDependents => dependents.HasDependents;

        readonly DependencyList dependents;
        readonly DisposableCollection disposables = new DisposableCollection();
        readonly SerializedDisposableCollection serializedDisposables = new SerializedDisposableCollection();

        readonly Action<UpdateHelper> updater;
        readonly Action noMoreDependencies;

        readonly UpdateHelper updateHelper;

        public DependencyHandler(IMaster master, Action<UpdateHelper> updater = null, Action noMoreDependencies = null)
        {
            dependents = new DependencyList(noMoreDependencies);
            this.Master = master;
            this.updater = updater;
            this.noMoreDependencies = noMoreDependencies;
            this.updateHelper = new UpdateHelper(this);
            MarkAsStale();
        }


        public IDisposable DependOn(IDependable dependable) => disposables.Add(dependable.AddDependent(this));

        public IDisposable DependOn(Action<UpdateHelper> onUpdate, IDependable dependable) =>
            disposables.Add(dependable.AddDependent(Utility2.CreateDependent(Master, () => onUpdate(updateHelper))));

        public IDisposable DependOn(params IDependable[] dependables)
        {
            var disposables = new DisposableCollection();
            foreach (var dependable in dependables)
                disposables.Add(DependOn(dependable));
            return disposables;
        }

        public IDisposable DependOn(Action<UpdateHelper> onUpdate, params IDependable[] dependables)
        {
            var disposables = new DisposableCollection();
            foreach (var dependable in dependables)
                disposables.Add(DependOn(onUpdate, dependable));
            return disposables;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public void MakeDependentsStale() => dependents.MarkAsStale();

        public T AddDisposable<T>(T disposable, DisposableLifetime lifetime = default(DisposableLifetime)) where T : IDisposable
        {
            if (lifetime == DisposableLifetime.UntilDeletion)
                disposables.Add(disposable);
            else if (lifetime == DisposableLifetime.UntilUpdate)
                serializedDisposables.Add(disposable);

            return disposable;
        }

        public T[] AddDisposables<T>(params T[] disposables) where T : IDisposable
        {
            foreach (var disposable in disposables)
                AddDisposable(disposable);

            return disposables;
        }

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => dependents.Add(dependent);

        // IDependent
        public void MarkAsStale()
        {
            MakeDependentsStale();
            serializedDisposables.Dispose();
            if (updater != null)
                Master.AddStaleObject(Utility2.CreateUpdatable(() => updater(updateHelper)));
        }

        // IDisposable
        public void Dispose()
        {
            serializedDisposables.Dispose();
            disposables.Dispose();
        }
    }

    class UpdateHelper
    {
        readonly DependencyHandler dependencyHandler;

        public UpdateHelper(DependencyHandler dependencyHandler) => this.dependencyHandler = dependencyHandler;

        [System.Diagnostics.DebuggerStepThrough]
        public void MakeDependentsStale() => dependencyHandler.MakeDependentsStale();
    }

    enum DisposableLifetime
    {
        UntilDeletion,
        UntilUpdate
    }
}