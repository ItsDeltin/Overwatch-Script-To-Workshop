using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Scopes
{
    class Scope
    {
        IEnumerable<IScopeSource> _sources;

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

        public ScopeWatcher Watch()
        {
            ScopeWatcher watcher = new ScopeWatcher();

            foreach (var source in _sources)
                watcher.SubscribeTo(source);

            return watcher;
        }

        public IDisposable WatchAndSubscribe(IObserver<ScopeSourceChange> observer)
        {
            var watcher = Watch();
            return new CompositeDisposable()
            {
                watcher,
                watcher.Subscribe(observer)
            };
        }

        public Scope CreateChild(IScopeSource scopeSource) => new Scope(this, scopeSource);

        public static readonly Scope Empty = new Scope();
    }
}