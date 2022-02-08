using System;

namespace DS.Analysis.Types
{
    using Core;

    interface ITypeDirector : IDependable
    {
        CodeType Type { get; }
    }

    interface IDisposableTypeDirector : ITypeDirector, IDisposable
    {
    }


    /// <summary>
    /// Enapsulates a ITypeDirector in a IDisposableTypeDirector
    /// </summary>
    class EmptyDisposableTypeDirector : IDisposableTypeDirector
    {
        readonly ITypeDirector typeDirector;
        public EmptyDisposableTypeDirector(ITypeDirector typeDirector) => this.typeDirector = typeDirector;

        public void Dispose() { }
        public IDisposable Subscribe(IObserver<CodeType> observer) => typeDirector.Subscribe(observer);
    }
}