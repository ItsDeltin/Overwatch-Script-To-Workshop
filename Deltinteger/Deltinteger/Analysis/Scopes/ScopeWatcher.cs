using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IScopeSource, IDisposable
    {
        readonly ValueObserverCollection<ScopeSourceChange> observers = new ValueObserverCollection<ScopeSourceChange>(ScopeSourceChange.Empty);
        readonly Dictionary<IScopeSource, SourceListenerInfo> subscriptions = new Dictionary<IScopeSource, SourceListenerInfo>();

        public void SubscribeTo(IScopeSource scopeSource)
        {
            // Create a SourceListenerInfo instance.
            var listenerInfo = new SourceListenerInfo();

            // Link the listenerInfo to the scopeSource.
            subscriptions.Add(scopeSource, listenerInfo);

            // Subscribe to the scope source.
            listenerInfo.SourceSubscription = scopeSource.Subscribe(change =>
            {
                listenerInfo.SetElements(change.Elements);
                Notify();
            });
        }

        public void UnsubscribeFrom(IScopeSource scopeSource)
        {
            subscriptions[scopeSource].Dispose();
            subscriptions.Remove(scopeSource);
            Notify();
        }

        void Notify()
        {
            var result = Enumerable.Empty<ScopedElement>();
            foreach (var subscription in subscriptions)
                result = result.Concat(subscription.Value.Elements);

            observers.Set(new ScopeSourceChange(result.ToArray()));
        }


        // IObservable<ScopeWatcherValue>
        public IDisposable Subscribe(IObserver<ScopeSourceChange> observer) => observers.Add(observer);


        // IDisposable
        public void Dispose()
        {
            foreach (var sub in subscriptions)
                sub.Value.Dispose();

            observers.Complete();
        }


        /// <summary>Contains data about a subscription to a Scope Source.</summary>
        class SourceListenerInfo : IDisposable
        {
            /// <summary>The subscription to the scope source.</summary>
            public IDisposable SourceSubscription { get; set; }

            /// <summary>The data retrieved from the source scope subscription.</summary>
            public ScopedElement[] Elements { get; private set; }

            public void SetElements(ScopedElement[] elements) => Elements = elements;

            public void Dispose() => SourceSubscription.Dispose();
        }
    }
}