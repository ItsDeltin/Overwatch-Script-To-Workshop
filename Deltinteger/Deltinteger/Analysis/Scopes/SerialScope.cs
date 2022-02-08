using System;

namespace DS.Analysis.Scopes
{
    using Core;
    using Utility;

    class SerialScope : IScopeSource
    {
        public ScopedElement[] Elements
        {
            get => _elements; set
            {
                _elements = value;
                dependents.MarkAsStale();
            }
        }

        ScopedElement[] _elements;

        readonly DependentCollection dependents = new DependentCollection();

        public SerialScope() { }

        public SerialScope(ScopedElement[] initialElements) { }

        public IDisposable AddDependent(IDependent dependent) => dependents.Add(dependent);
    }
}