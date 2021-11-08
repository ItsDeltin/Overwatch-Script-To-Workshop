using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IObservable<ScopeWatcherValue>, IDisposable
    {
        readonly ValueObserverCollection<ScopeWatcherValue> observers = new ValueObserverCollection<ScopeWatcherValue>(new ScopeWatcherValue(new ScopedElementData[0]));
        readonly Dictionary<IScopeSource, SourceListenerInfo> subscriptions = new Dictionary<IScopeSource, SourceListenerInfo>();

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

            bool isSubscribing = false;

            readonly ScopeWatcher watcher;

            public SourceListenerInfo(ScopeWatcher watcher) => this.watcher = watcher;

            public void SetElements(ScopedElement[] elements)
            {
                Elements = elements;

                // Dispose old element subscriptions.
                DisposeElementSubscriptions();
                
                // Add new element subscriptions
                elementSubscriptions = new IDisposable[Elements.Length];
                ScopedElementData = new ScopedElementData[Elements.Length];
                isSubscribing = true;
                for (int i = 0; i < Elements.Length; i++)
                {
                    int captureI = i;

                    // Subscribe to the scoped element
                    elementSubscriptions[i] = Elements[i].Subscribe(scopedElementData => {
                        ScopedElementData[captureI] = scopedElementData;
                        // When we subscribe to the ScopedElement, this block is called immediately.
                        // Don't notify while we are in the middle of subscribing.
                        if (!isSubscribing)
                            watcher.Notify();
                    });
                }
                isSubscribing = false;
                watcher.Notify();
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