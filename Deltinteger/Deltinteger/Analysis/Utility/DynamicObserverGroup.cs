using System;
using System.Reactive.Disposables;

namespace DS.Analysis.Utility
{
    static class DynamicObserverGroup
    {
        public static IDisposable Create(Action callback, params DynamicUpdatable<object>[] updatables)
        {
            foreach (var updatable in updatables)
                updatable.SetCallback(callback);
            return new CompositeDisposable(updatables);
        }
    }

    class DynamicUpdatable<T> : IDisposable
    {
        public T Value
        {
            get
            {
                ThrowIfDisposed();
                return _value;
            }
        }
        public bool TriggerCallback { get; set; } = true;


        readonly IObservable<T> observable;
        readonly bool expectInitialValue;
        readonly bool throwIfNull;


        T _value;

        IDisposable subscription;
        Action callback;
        bool disposed;


        public DynamicUpdatable(IObservable<T> observable, bool expectInitialValue = true, bool throwIfNull = true, bool triggerCallback = true)
        {
            this.observable = observable ?? throw new ArgumentNullException(nameof(observable));
            this.expectInitialValue = expectInitialValue;
            this.throwIfNull = throwIfNull;
            TriggerCallback = triggerCallback;
        }

        public void SetCallback(Action callback)
        {
            ThrowIfDisposed();

            // 'SetCallback' was called twice.
            if (callback != null)
                throw new Exception("callback already set");

            this.callback = callback ?? throw new NullReferenceException(nameof(callback));

            // Get the subcription.
            bool initialized = false;
            subscription = observable.Subscribe(newValue =>
            {
                // Update value.
                initialized = true;
                _value = newValue;

                // Throw if null.
                if (_value == null && throwIfNull)
                    throw new Exception(ToString() + " was notified of a null value");

                // Execute the callback.
                if (TriggerCallback)
                    callback();
            });

            // 'Value' was not initialized.
            if (expectInitialValue && !initialized)
                throw new Exception(ToString() + " was not initialized");
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            disposed = true;
            subscription.Dispose();
        }

        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(ToString());
        }
    }
}