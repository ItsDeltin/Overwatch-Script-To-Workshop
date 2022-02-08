using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Utility
{
    using Core;

    class DependentCollection
    {
        readonly HashSet<IDependent> dependents = new HashSet<IDependent>();
        readonly Action onEmpty;

        public DependentCollection() { }

        public DependentCollection(Action onEmpty)
        {
            this.onEmpty = onEmpty;
        }

        public IDisposable Add(IDependent dependent)
        {
            dependents.Add(dependent);

            // Return an action that will remove the dependency.
            return Disposable.Create(() =>
            {
                if (!dependents.Remove(dependent))
                    throw new ObjectDisposedException(
                        message: "The dependency was already removed.",
                        innerException: null);

                // Call onEmpty if there are no more dependencies.
                if (dependents.Count == 0 && onEmpty != null)
                    onEmpty();
            });
        }

        public void MarkAsStale()
        {
            foreach (var dependent in dependents)
                dependent.MarkAsStale();
        }
    }
}