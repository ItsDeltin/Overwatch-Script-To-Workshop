namespace DS.Model;
using System;
using System.Reactive.Disposables;

static class DisposableExtensions
{
    public static IDisposable Append(this IDisposable disposable, Action action)
    {
        return new CompositeDisposable(new[] {
            disposable,
            Disposable.Create(action)
        });
    }
}