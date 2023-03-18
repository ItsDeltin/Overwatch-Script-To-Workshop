using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Core
{
    class DependencyList
    {
        public bool HasDependents => dependents.Count != 0;

        readonly HashSet<IDependent> dependents = new HashSet<IDependent>();
        readonly string name;
        readonly Action noMoreDependencies;

        public DependencyList(string name, Action noMoreDependencies = null)
        {
            this.name = name;
            this.noMoreDependencies = noMoreDependencies;
        }

        public IDisposable Add(IDependent dependent)
        {
            // Make the dependent stale.
            dependent.MarkAsStale(name);

            // Add the dependent to the list of dependents.
            dependents.Add(dependent);

            // When disposed, this will unlink the dependency.
            return Disposable.Create(() =>
            {
                if (dependents.Remove(dependent) && dependents.Count == 0 && noMoreDependencies != null)
                    noMoreDependencies();
            });
        }

        [System.Diagnostics.DebuggerStepThrough]
        public void MarkAsStale()
        {
            foreach (var dependent in dependents)
                dependent.MarkAsStale(name);
        }
    }
}