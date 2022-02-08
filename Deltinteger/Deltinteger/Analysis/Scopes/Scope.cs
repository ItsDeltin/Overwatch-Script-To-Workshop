using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Scopes
{
    using Core;

    class Scope
    {
        readonly IEnumerable<IScopeSource> _sources;
        ScopeWatcher watcher;

        public Scope()
        {
            _sources = Enumerable.Empty<IScopeSource>();
        }

        public Scope(IScopeSource source)
        {
            _sources = new[] { source };
        }

        public Scope(params IScopeSource[] sources)
        {
            _sources = sources;
        }

        public Scope(Scope parent, IScopeSource source)
        {
            _sources = parent._sources.Append(source);
        }

        public ScopeWatcher Watch(IMaster master)
        {
            if (watcher == null)
            {
                watcher = new ScopeWatcher(master, () =>
                {
                    watcher.Dispose();
                    watcher = null;
                });

                foreach (var source in _sources)
                    watcher.SubscribeTo(source);
            }

            return watcher;
        }

        public Scope CreateChild(IScopeSource scopeSource) => new Scope(this, scopeSource);

        public static readonly Scope Empty = new Scope();
    }
}