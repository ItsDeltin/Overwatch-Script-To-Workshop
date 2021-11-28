using System;

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
        public static IDisposable Select<TIn, TOut>(this IObservable<TIn> source, IObserver<TOut> output, Func<TIn, TOut> selector) => source.Subscribe(
            onNext => output.OnNext(selector(onNext)),
            onError => output.OnError(onError),
            () => output.OnCompleted()
        );
    }
}