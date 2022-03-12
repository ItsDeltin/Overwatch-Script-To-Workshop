using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using DS.Analysis.Utility;
using DS.Analysis.Core;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IScopeSource
    {
        // IScopeSource
        public ScopedElement[] Elements { get; private set; }

        readonly IMaster master;
        readonly IEnumerable<IScopeSource> sources;
        readonly DependencyHandler dependencyHandler;

        public ScopeWatcher(IMaster master, IEnumerable<IScopeSource> sources)
        {
            this.master = master;
            this.sources = sources;
            dependencyHandler = new DependencyHandler(master, update =>
            {
                // Concat all sources
                var result = Enumerable.Empty<ScopedElement>();
                foreach (var source in sources)
                    result = result.Concat(source.Elements);

                Elements = result.ToArray();

                update.MakeDependentsStale();
            });

            foreach (var source in sources)
                dependencyHandler.DependOn(source);
        }

        public ScopeWatcher CreateChild(IScopeSource scopeSource) => new ScopeWatcher(master, sources.Append(scopeSource));

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => dependencyHandler.AddDependent(dependencyHandler);
    }
}