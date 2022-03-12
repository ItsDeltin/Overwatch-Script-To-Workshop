using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Scopes
{
    using Core;

    class Scope : IDependable
    {
        public ScopedElement[] Elements { get; private set; }

        readonly IEnumerable<IScopeSource> sources;
        readonly DependencyHandler dependencyHandler;

        public Scope()
        {
            sources = Enumerable.Empty<IScopeSource>();
        }

        public Scope(IMaster master, params IScopeSource[] sources)
        {
            this.sources = sources;
            dependencyHandler = new DependencyHandler(master, Update);
            Subscribe();
        }

        private Scope(IMaster master, Scope parent, IScopeSource source)
        {
            sources = parent.sources.Append(source);
            dependencyHandler = new DependencyHandler(master, Update);
        }


        void Subscribe()
        {
            foreach (var source in sources)
                dependencyHandler.DependOn(source);
        }

        void Update(UpdateHelper updater)
        {
            var elements = Enumerable.Empty<ScopedElement>();

            foreach (var source in sources)
                elements = elements.Concat(source.Elements);

            Elements = elements.ToArray();
            updater.MakeDependentsStale();
        }

        public Scope CreateChild(IMaster master, IScopeSource scopeSource) => new Scope(master, this, scopeSource);

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => dependencyHandler.AddDependent(dependent);

        public static readonly Scope Empty = new Scope();
    }
}