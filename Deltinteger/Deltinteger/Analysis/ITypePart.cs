using System;
using System.Reactive.Disposables;

namespace DS.Analysis
{
    using Core;
    using Types;
    using Types.Standard;
    using Types.Semantics;
    using Scopes;

    /// <summary>
    /// Handles element usage in a type tree.
    /// </summary>
    interface ITypePartHandler
    {
        bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount);
        TypePartInfoResult GetPartInfo(ProviderArguments arguments);
    }

    struct TypePartInfoResult
    {
        public readonly ITypePartInfo TypePartInfo;
        public readonly IDisposable Disposable;

        public TypePartInfoResult(ITypePartInfo typePartInfo, IDisposable disposable)
        {
            TypePartInfo = typePartInfo;
            Disposable = disposable;
        }
    }

    interface ITypePartInfo : IDependable
    {
        public IScopeSource ScopeSource { get; }
        public CodeType Type { get; }
        public IParentElement ParentElement { get; }
    }

    struct TypePartInfo
    {
        public readonly IScopeSource ScopeSource;
        public readonly CodeType Type;
        public readonly IParentElement ParentElement;

        public TypePartInfo(CodeType type)
        {
            ScopeSource = type.Content.ScopeSource;
            Type = type;
            ParentElement = type;
        }

        public TypePartInfo(IParentElement parentElement, IScopeSource scopeSource)
        {
            ScopeSource = scopeSource;
            Type = null;
            ParentElement = parentElement;
        }
    }

    class SerialTypePartInfo : ITypePartInfo
    {
        public IScopeSource ScopeSource
        {
            get => scopeSource;
            set
            {
                scopeSource = value;
                dependencyList.MarkAsStale();
            }
        }

        public CodeType Type
        {
            get => type;
            set
            {
                type = value;
                dependencyList.MarkAsStale();
            }
        }

        public IParentElement ParentElement
        {
            get => parentElement;
            set
            {
                parentElement = value;
                dependencyList.MarkAsStale();
            }
        }

        // Backing variables
        IScopeSource scopeSource;
        CodeType type;
        IParentElement parentElement;


        public SerialTypePartInfo() { }

        public SerialTypePartInfo(IScopeSource scopeSource, CodeType type, IParentElement parentElement)
        {
            this.scopeSource = scopeSource;
            this.type = type;
            this.parentElement = parentElement;
        }

        public SerialTypePartInfo(IScopeSource scopeSource, IParentElement parentElement)
        {
            this.scopeSource = scopeSource;
            this.parentElement = parentElement;
        }

        readonly DependencyList dependencyList = new DependencyList();

        public IDisposable AddDependent(IDependent dependent) => dependencyList.Add(dependent);
    }


    /// <summary>
    /// Invalid type part handler.
    /// </summary>
    class UnknownTypePartHandler : ITypePartHandler
    {
        public static readonly UnknownTypePartHandler Instance = new UnknownTypePartHandler();

        public TypePartInfo PartInfo { get; } = new TypePartInfo(StandardTypes.Unknown.Instance);

        public IDisposable AddDependent(IDependent dependent) => Disposable.Empty;

        public bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount) => false;
    }
}