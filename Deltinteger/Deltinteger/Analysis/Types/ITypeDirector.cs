using System;

namespace DS.Analysis.Types
{
    using Core;
    using Utility;

    interface ITypeDirector : IDependable
    {
        CodeType Type { get; }
    }

    interface IDisposableTypeDirector : ITypeDirector, IDisposable
    {
    }

    class SerialDisposableTypeDirector : IDisposableTypeDirector
    {
        public CodeType Type
        {
            get => type ?? StandardType.Unknown.Instance;
            set
            {
                type = value;
                dependents.MarkAsStale();
            }
        }

        CodeType type;

        readonly IDisposable disposable;

        readonly DependencyList dependents = new DependencyList();

        public IDisposable AddDependent(IDependent dependent) => dependents.Add(dependent);

        public SerialDisposableTypeDirector(IDisposable disposable) => this.disposable = disposable;

        public void Dispose() => disposable.Dispose();
    }


    /// <summary>
    /// Enapsulates a ITypeDirector in a IDisposableTypeDirector
    /// </summary>
    class EmptyDisposableTypeDirector : IDisposableTypeDirector
    {
        readonly ITypeDirector typeDirector;
        public EmptyDisposableTypeDirector(ITypeDirector typeDirector) => this.typeDirector = typeDirector;

        public CodeType Type => typeDirector.Type;
        public IDisposable AddDependent(IDependent dependent) => typeDirector.AddDependent(dependent);

        public void Dispose() { }
    }
}