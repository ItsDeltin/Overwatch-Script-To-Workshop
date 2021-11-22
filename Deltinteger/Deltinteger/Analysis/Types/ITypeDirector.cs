using System;

namespace DS.Analysis.Types
{
    interface ITypeDirector : IObservable<CodeType>
    {
    }

    interface IDisposableTypeDirector : ITypeDirector, IDisposable
    {
    }
}