using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IObservable<ScopeWatcherValue>, IDisposable
    {
        public ScopeWatcherParameters Parameters { get; }

        readonly List<AbstractScopeSource.AbstractUnsubscriber> _sourceUnsubscribers = new List<AbstractScopeSource.AbstractUnsubscriber>();
        readonly List<ScopedElement> _elements = new List<ScopedElement>();
        readonly ObserverCollection<ScopeWatcherValue> _observers = new ObserverCollection<ScopeWatcherValue>();

        public IReadOnlyList<ScopedElement> Matched => _elements;

        public ScopeWatcher(ScopeWatcherParameters parameters)
        {
            Parameters = parameters;
        }

        public void SubscribeTo(ScopeSource scopeSource)
        {
            _sourceUnsubscribers.Add(scopeSource.Subscribe(new SourceListener(this)));
        }

        public void Dispose()
        {
            foreach (var unsubscriber in _sourceUnsubscribers)
                unsubscriber.Dispose();
            
            _observers.Complete();
        }

        public IDisposable Subscribe(IObserver<ScopeWatcherValue> observer)
        {
            observer.OnNext(new ScopeWatcherValue(Matched.ToArray()));
            return _observers.Add(observer);
        }

        class SourceListener : IScopeSourceListener
        {
            readonly ScopeWatcher _watcher;
            public SourceListener(ScopeWatcher watcher) => _watcher = watcher;
            public void Notify(ScopedElement element)
            {
                _watcher._elements.Add(element);
                _watcher._observers.Set(new ScopeWatcherValue(_watcher.Matched.ToArray()));
            }
        }
    }

    struct ScopeWatcherValue
    {
        public ScopedElement[] FoundElements;

        public ScopeWatcherValue(ScopedElement[] foundElements)
        {
            FoundElements = foundElements;
        }
    }
}