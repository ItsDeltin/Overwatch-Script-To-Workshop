using System;
using System.Reactive.Disposables;

namespace DS.Analysis.Scopes
{
    using Types;
    using Types.Standard;
    using Types.Semantics;
    using Utility;

    /// <summary>
    /// Handles element usage in a type tree.
    /// </summary>
    interface ITypePartHandler
    {
        bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount);
        IDisposable Get(IObserver<TypePartResult> observer, ProviderArguments arguments);
    }

    struct TypePartResult
    {
        public readonly Scope Scope;
        public readonly CodeType Type;
        public readonly IParentElement ParentElement;

        public TypePartResult(CodeType type)
        {
            Type = type;
            ParentElement = type;
            Scope = new Scope(type.Content.ScopeSource);
        }

        public TypePartResult(IParentElement parentElement, Scope scope)
        {
            Scope = scope;
            Type = null;
            ParentElement = parentElement;
        }
    }


    /// <summary>
    /// Invalid type part handler.
    /// </summary>
    class UnknownTypePartHandler : ITypePartHandler
    {
        public static readonly UnknownTypePartHandler Instance = new UnknownTypePartHandler();

        public bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount) => false;
        public IDisposable Get(IObserver<TypePartResult> observer, ProviderArguments arguments)
        {
            observer.OnNext(new TypePartResult(StandardTypes.Unknown.Instance));
            return Disposable.Empty;
        }
    }
}