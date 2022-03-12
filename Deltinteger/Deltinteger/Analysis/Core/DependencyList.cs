using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Core
{
    class DependencyList
    {
        public bool HasDependents => dependents.Count != 0;

        readonly HashSet<IDependent> dependents = new HashSet<IDependent>();
        readonly Action noMoreDependencies;

        public DependencyList(Action noMoreDependencies = null)
        {
            this.noMoreDependencies = noMoreDependencies;
        }

        public IDisposable Add(IDependent dependent)
        {
            dependents.Add(dependent);
            return Disposable.Create(() =>
            {
                if (dependents.Remove(dependent) && dependents.Count == 0)
                    noMoreDependencies?.Invoke();
            });
        }

        public void MarkAsStale()
        {
            foreach (var dependent in dependents)
                dependent.MarkAsStale();
        }
    }
}