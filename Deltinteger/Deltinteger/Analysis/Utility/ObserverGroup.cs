using System;
using System.Linq;

namespace DS.Analysis.Utility
{
    /// <summary>
    /// Watches multiple observables and combines their broadcasted values into a single event
    /// </summary>
    class ObserverGroup<T> : IDisposable
    {
        readonly Func<T[], IDisposable> callback;
        readonly IDisposable[] subscriptions;
        readonly T[] values;
        readonly bool throwIfNull = true;
        IDisposable providedDisposable;
        bool subscribed;
        bool disposed;

        public ObserverGroup(Func<T[], IDisposable> callback, params Func<Action<T>, IDisposable>[] getSubscriptions)
        {
            this.callback = callback;
            subscriptions = new IDisposable[getSubscriptions.Length];
            values = new T[getSubscriptions.Length];

            // Get the subscriptions
            for (int i = 0; i < subscriptions.Length; i++)
            {
                // The value of 'i' will change in the lambda, store in a new variable.
                int captureI = i;
                // Get the subscription
                subscriptions[i] = getSubscriptions[i](newValue =>
                {
                    if (throwIfNull && newValue == null)
                        throw new Exception("Observable broadcasted a null value");

                    values[captureI] = newValue;
                    // Execute the callback if subscribing has completed.
                    if (subscribed)
                        Update();
                });

                if (throwIfNull && values[i] == null)
                    throw new Exception("ObserverGroup value not initialized when subscribed");
            }

            subscribed = true;
            // Initial update
            Update();
        }

        void Update()
        {
            ThrowIfDisposed();

            if (subscribed)
            {
                providedDisposable?.Dispose();
                providedDisposable = callback(values);
            }
        }


        public void Dispose()
        {
            ThrowIfDisposed();

            providedDisposable?.Dispose();
            foreach (var sub in subscriptions)
                sub.Dispose();

            disposed = true;
        }

        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("ObserverGroup already disposed");
        }
    }
}