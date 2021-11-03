using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IObservable<ScopeWatcherValue>, IDisposable
    {
        public ScopeWatcherParameters Parameters { get; }

        readonly ObserverCollection<ScopeWatcherValue> observers = new ObserverCollection<ScopeWatcherValue>();
        readonly Dictionary<IScopeSource, SourceListenerInfo> subscriptions = new Dictionary<IScopeSource, SourceListenerInfo>();

        ScopedElement[] current = new ScopedElement[0];

        public ScopeWatcher(ScopeWatcherParameters parameters)
        {
            Parameters = parameters;
        }

        public void SubscribeTo(IScopeSource scopeSource)
        {
            // Create a SourceListenerInfo instance.
            var listenerInfo = new SourceListenerInfo();

            // Link the listenerInfo to the scopeSource.
            subscriptions.Add(scopeSource, listenerInfo);

            // Subscribe to the scope source.
            listenerInfo.Subscription = scopeSource.Subscribe(change => {
                listenerInfo.Elements = change.Elements;
                Notify();
            });
        }

        void Notify()
        {
            var result = Enumerable.Empty<ScopedElement>();
            foreach (var subscription in subscriptions)
                result = result.Concat(subscription.Value.Elements);
            
            current = result.ToArray();
            observers.Set(new ScopeWatcherValue(current));
        }


        // IObservable<ScopeWatcherValue>
        public IDisposable Subscribe(IObserver<ScopeWatcherValue> observer)
        {
            observer.OnNext(new ScopeWatcherValue(current));
            return observers.Add(observer);
        }


        // IDisposable
        public void Dispose()
        {
            foreach (var sub in subscriptions)
                sub.Value.Subscription.Dispose();
            
            observers.Complete();
        }


        class SourceListenerInfo
        {
            public IDisposable Subscription;
            public ScopedElement[] Elements;
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