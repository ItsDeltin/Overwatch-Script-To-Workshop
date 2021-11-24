using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Utility
{
    /// <summary>Contains a list of IObservables which can notified collectively.</summary>
    /// <typeparam name="T">The type of the IObserver value.</typeparam>
    class ObserverCollection<T> : IObservable<T>, IDisposable
    {
        readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

        /// <summary>Adds an observer to the collection.</summary>
        /// <param name="observer">The observer that is added to the collection.</param>
        /// <returns>An IDisposable that when disposed will remove the observer from the collection.</returns>
        public virtual IDisposable Add(IObserver<T> observer)
        {
            _observers.Add(observer);
            return Disposable.Create(() => _observers.Remove(observer));
        }

        /// <summary>Pushes a new value to the observers.</summary>
        /// <param name="value">The current notification information. </param>
        public virtual void Set(T value)
        {
            foreach (var observer in _observers)
                observer.OnNext(value);
        }

        /// <summary>Notifies the observers that the provider has experienced an error condition.</summary>
        /// <param name="exception">An object that provides additional information about the error.</param>
        public void OnError(Exception exception)
        {
            foreach (var observer in _observers)
                observer.OnError(exception);
        }

        /// <summary>Notifies the observers that the provider has finished sending push-based notifications.</summary>
        public void Complete()
        {
            foreach (var observer in _observers)
                observer.OnCompleted();
        }

        public bool Any() => _observers.Count != 0;

        void IDisposable.Dispose() => Complete();

        IDisposable IObservable<T>.Subscribe(IObserver<T> observer) => Add(observer);
    }

    /// <summary>An ObserverCollection that keeps the last pushed value and immediately notifies new subscribers of the current value.</summary>
    /// <typeparam name="T"></typeparam>
    class ValueObserverCollection<T> : ObserverCollection<T>
    {
        public T Value { get; private set; }

        public ValueObserverCollection() { }

        public ValueObserverCollection(T initialValue) => Value = initialValue;

        public override IDisposable Add(IObserver<T> observer)
        {
            observer.OnNext(Value);
            return base.Add(observer);
        }

        public override void Set(T value)
        {
            Value = value;
            base.Set(value);
        }
    }
}