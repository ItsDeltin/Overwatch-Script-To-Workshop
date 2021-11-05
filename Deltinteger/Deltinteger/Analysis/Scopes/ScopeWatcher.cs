using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IObservable<ScopeWatcherValue>, IDisposable
    {
        public ScopeWatcherParameters Parameters { get; }

        readonly ValueObserverCollection<ScopeWatcherValue> observers = new ValueObserverCollection<ScopeWatcherValue>(new ScopeWatcherValue(new ScopedElementData[0]));
        readonly Dictionary<IScopeSource, SourceListenerInfo> subscriptions = new Dictionary<IScopeSource, SourceListenerInfo>();

        public ScopeWatcher(ScopeWatcherParameters parameters)
        {
            Parameters = parameters;
        }

        public void SubscribeTo(IScopeSource scopeSource)
        {
            // Create a SourceListenerInfo instance.
            var listenerInfo = new SourceListenerInfo(this);

            // Link the listenerInfo to the scopeSource.
            subscriptions.Add(scopeSource, listenerInfo);

            // Subscribe to the scope source.
            listenerInfo.SourceSubscription = scopeSource.Subscribe(change => {
                listenerInfo.SetElements(change.Elements);
            });
        }

        void Notify()
        {
            var result = Enumerable.Empty<ScopedElementData>();
            foreach (var subscription in subscriptions)
                result = result.Concat(subscription.Value.ScopedElementData);
            
            observers.Set(new ScopeWatcherValue(result.ToArray()));
        }


        // IObservable<ScopeWatcherValue>
        public IDisposable Subscribe(IObserver<ScopeWatcherValue> observer) => observers.Add(observer);


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

            /// <summary>The subscriptions to the Elements. Will be the same length as Elements.</summary>
            IDisposable[] elementSubscriptions;

            /// <summary>The data retrieved from the element subscriptions.</summary>
            public ScopedElementData[] ScopedElementData { get; private set; }

            readonly ScopeWatcher watcher;

            public SourceListenerInfo(ScopeWatcher watcher) => this.watcher = watcher;

            public void SetElements(ScopedElement[] elements)
            {
                Elements = elements;

                // Dispose old element subscriptions.
                DisposeElementSubscriptions();
                
                // Add new element subscriptions
                elementSubscriptions = new IDisposable[Elements.Length];
                for (int i = 0; i < Elements.Length; i++)
                {
                    int captureI = i;

                    // Subscribe to the scoped element
                    elementSubscriptions[i] = Elements[i].Subscribe(scopedElementData => {
                        ScopedElementData[captureI] = scopedElementData;
                        watcher.Notify();
                    });
                }
            }

            public void Dispose()
            {
                SourceSubscription.Dispose();
                DisposeElementSubscriptions();
            }

            void DisposeElementSubscriptions()
            {
                if (elementSubscriptions != null)
                    foreach (var elementSub in elementSubscriptions)
                        elementSub.Dispose();
            }
        }
    }

    struct ScopeWatcherValue
    {
        public ScopedElementData[] FoundElements;

        public ScopeWatcherValue(ScopedElementData[] foundElements)
        {
            FoundElements = foundElements;
        }
    }
}