using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;

namespace DS.Analysis.Utility
{
    using Types;
    using Types.Standard;

    /// <summary>Contains helper methods for DeltinScript analysis.</summary>
    static class Helper
    {
        /// <summary>Creates a new CodeType ObserverCollection initialized with StandardTypes.Unknown.Instance.</summary>
        /// <returns>The newly initialized CodeType ObserverCollection.</returns>
        public static ObserverCollection<CodeType> CreateTypeObserver() => new ValueObserverCollection<CodeType>(StandardTypes.Unknown.Instance);


        /// <summary>Subscribes an observer of differing type to an observable sequence.</summary>
        /// <param name="source">The observable sequence that will be subscribed to.</param>
        /// <param name="output">The observer which the observable will broadcast notifications to.</param>
        /// <param name="selector">A transform function to apply to each new value.</param>
        /// <typeparam name="TIn">The type of the source observable.</typeparam>
        /// <typeparam name="TOut">The type of the output observer.</typeparam>
        /// <returns>IDisposable object used to unsubscribe from the observable sequence.</returns>
        public static IDisposable Select<TIn, TOut>(this IObservable<TIn> source, IObserver<TOut> output, Func<TIn, TOut> selector) => source.Subscribe(output.Convert(selector));

        /// <summary>Creates an observer that will pass its values to the source observer.</summary>
        /// <param name="source">The source observable where values will be broadcasted to through the new observer.</param>
        /// <param name="selector">The selector that converts the input TIn value to a TOut value that is subsequently passed to the observer.</param>
        /// <typeparam name="TIn">Type of the new observer which contains the source observer.</typeparam>
        /// <typeparam name="TOut">The type of the source observer.</typeparam>
        /// <returns>A new IObservable which will pass the provided values filtered with the selector to the source IObservable.</returns>
        public static IObserver<TIn> Convert<TIn, TOut>(this IObserver<TOut> source, Func<TIn, TOut> selector) => Observer.Create<TIn>(
            onNext => source.OnNext(selector(onNext)),
            onError => source.OnError(onError),
            () => source.OnCompleted()
        );


        /// <summary>Observes a collection of observables. If any of the observables broadcasts a new value, the callback is triggered.</summary>
        /// <param name="observables">The observables that will be subscribed to and watched.</param>
        /// <param name="callback">The event to trigger when any of the observables provide a new value. An IDisposable can be returned which will be disposed
        /// when the event is triggered again or the IDisposable created by this method is disposed.</param>
        /// <typeparam name="T">The type of the observable values.</typeparam>
        /// <returns>IDisposable object which when disposed will unsubscribe from the observables and dispose of any additional data created by the callback.</returns>
        public static IDisposable Observe<T>(IEnumerable<IObservable<T>> observables, Func<T[], IDisposable> callback) =>
            new ObserverGroup<T>(callback, observables.Select<IObservable<T>, Func<Action<T>, IDisposable>>(observer => set => observer.Subscribe(value => set(value))).ToArray());

        /// <summary>Observes a collection of observables. If any of the observables broadcasts a new value, the callback is triggered.</summary>
        /// <param name="observables">The observables that will be subscribed to and watched.</param>
        /// <param name="callback">The event to trigger when any of the observables provide a new value.</param>
        /// <typeparam name="T">The type of the observable values.</typeparam>
        /// <returns>IDisposable object which when disposed will unsubscribe from the observables.</returns>
        public static IDisposable Observe<T>(IEnumerable<IObservable<T>> observables, Action<T[]> callback) =>
            Observe(observables, values => { callback(values); return null; });

        static IDisposable Observe(Func<object[], IDisposable> callback, params Func<Action<object>, IDisposable>[] getSubscriptions) =>
            new ObserverGroup<object>(callback, getSubscriptions);

        /// <summary>Watches 2 observables with types A and B. If any of them broadcasts a new value, the callback is triggered.</summary>
        /// <param name="observerA">The first observable.</param>
        /// <param name="observerB">The second observable.</param>
        /// <param name="callback">The event to trigger when any of the observables provide a new value. An IDisposable can be returned which will be disposed
        /// when the event is triggered again or the IDisposable created by this method is disposed.</param>
        /// <typeparam name="A">The type of the first observable.</typeparam>
        /// <typeparam name="B">The type of the second observable.</typeparam>
        /// <returns>IDisposable object which when disposed will unsubscribe from the observables and dispose of any additional data created by the callback.</returns>
        public static IDisposable Observe<A, B>(IObservable<A> observerA, IObservable<B> observerB, Func<A, B, IDisposable> callback) =>
            Observe(
                values => callback((A)values[0], (B)values[1]),
                set => observerA.Subscribe(v => set(v)),
                set => observerB.Subscribe(v => set(v))
            );

        /// <summary>Watches 3 observables with types A, B, and C. If any of them broadcasts a new value, the callback is triggered.</summary>
        /// <param name="observerA">The first observable.</param>
        /// <param name="observerB">The second observable.</param>
        /// <param name="observerC">The third observable.</param>
        /// <param name="callback">The event to trigger when any of the observables provide a new value. An IDisposable can be returned which will be disposed
        /// when the event is triggered again or the IDisposable created by this method is disposed.</param>
        /// <typeparam name="A">The type of the first observable.</typeparam>
        /// <typeparam name="B">The type of the second observable.</typeparam>
        /// <typeparam name="C">The type of the third observable.</typeparam>
        /// <returns>IDisposable object which when disposed will unsubscribe from the observables and dispose of any additional data created by the callback.</returns>
        public static IDisposable Observe<A, B, C>(IObservable<A> observerA, IObservable<B> observerB, IObservable<C> observerC, Func<A, B, C, IDisposable> callback) =>
            Observe(
                values => callback((A)values[0], (B)values[1], (C)values[2]),
                set => observerA.Subscribe(v => set(v)),
                set => observerB.Subscribe(v => set(v)),
                set => observerC.Subscribe(v => set(v))
            );


        /// <summary>Disposes a collection of disposables.</summary>
        /// <param name="collection">The collection to dispose.</param>
        public static void Dispose(this IEnumerable<IDisposable> collection)
        {
            foreach (var value in collection)
                value.Dispose();
        }
    }
}