using System;
using System.Reactive.Disposables;

namespace DS.Analysis.Utility
{
    /// <summary>
    /// Watches multiple observables and combines their broadcasted values into a single event
    /// </summary>
    class ObserverGroup : IDisposable
    {
        readonly Func<object[], IDisposable> callback;
        readonly IDisposable[] subscriptions;
        readonly object[] values;
        readonly bool throwIfNull = true;
        IDisposable providedDisposable;
        bool subscribed;
        bool disposed;

        ObserverGroup(Func<object[], IDisposable> callback, params Func<Action<object>, IDisposable>[] getSubscriptions)
        {
            this.callback = callback;
            subscriptions = new IDisposable[getSubscriptions.Length];
            values = new object[getSubscriptions.Length];

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
            if (disposed)
                throw new ObjectDisposedException(nameof(ObserverGroup));

            if (subscribed)
            {
                providedDisposable?.Dispose();
                providedDisposable = callback(values);
            }
        }


        public void Dispose()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ObserverGroup));

            providedDisposable?.Dispose();
            foreach (var sub in subscriptions)
                sub.Dispose();

            disposed = true;
        }


        /// <summary>Watches multiple observables. If any of them broadcasts a new value, the callback is triggered.</summary>
        /// <param name="observerA">The first observable.</param>
        /// <param name="observerB">The second observable.</param>
        /// <param name="callback">The event to trigger when any of the observables provide a new value. An IDisposable can be returned which will be disposed
        /// when the event is triggered again or the IDisposable created by this method is disposed.</param>
        /// <typeparam name="A">The type of the first observable.</typeparam>
        /// <typeparam name="B">The type of the second observable.</typeparam>
        /// <returns>IDisposable object which when disposed will unsubscribe from the observables and dispose of any additional data created by the callback.</returns>
        public static IDisposable Observe<A, B>(IObservable<A> observerA, IObservable<B> observerB, Func<A, B, IDisposable> callback) =>
            new ObserverGroup(
                values => callback((A)values[0], (B)values[1]),
                set => observerA.Subscribe(v => set(v)),
                set => observerB.Subscribe(v => set(v))
            );

        public static IDisposable Observe<A, B, C>(IObservable<A> observerA, IObservable<B> observerB, IObservable<C> observerC, Func<A, B, C, IDisposable> callback) =>
            new ObserverGroup(
                values => callback((A)values[0], (B)values[1], (C)values[2]),
                set => observerA.Subscribe(v => set(v)),
                set => observerB.Subscribe(v => set(v)),
                set => observerC.Subscribe(v => set(v))
            );

        // Action callbacks
        public static IDisposable Observe<A, B>(IObservable<A> observerA, IObservable<B> observerB, Action<A, B> callback) =>
            Observe(observerA, observerB, (a, b) =>
            {
                callback(a, b);
                return Disposable.Empty;
            });

        public static IDisposable Observe<A, B, C>(IObservable<A> observerA, IObservable<B> observerB, IObservable<C> observerC, Action<A, B, C> callback) =>
            Observe(observerA, observerB, observerC, (a, b, c) =>
            {
                callback(a, b, c);
                return Disposable.Empty;
            });
    }
}