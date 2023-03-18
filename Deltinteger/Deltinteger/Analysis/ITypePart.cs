using System;
using System.Reactive.Disposables;

namespace DS.Analysis
{
    using Core;
    using Types;
    using Types.Semantics;
    using Scopes;

    /// <summary>
    /// Handles element usage in a type tree.
    /// </summary>
    interface ITypeNodeManager
    {
        bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount);
        ITypeNodeInstance GetPartInfo(ProviderArguments arguments);
    }

    interface ITypeNodeInstance : IDependable, IDisposable
    {
        // The scope of the type for the next part in the type tree.
        public IScopeSource ScopeSource { get; }
        // The CodeType that this points to. May be null.
        public CodeType Type { get; }
        // The parenting element.
        public IParentElement ParentElement { get; }
    }

    class SerialTypePartInfo : ITypeNodeInstance
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

        readonly DependencyList dependencyList = new DependencyList("SerialTypePartInfo");

        public IDisposable AddDependent(IDependent dependent) => dependencyList.Add(dependent);

        public void Dispose() { }
    }


    /// <summary>
    /// Invalid type part handler.
    /// </summary>
    class UnknownTypePartHandler : ITypeNodeManager, ITypeNodeInstance
    {
        public static readonly UnknownTypePartHandler Instance = new UnknownTypePartHandler();

        public ITypeNodeInstance GetPartInfo(ProviderArguments arguments) => this;

        public bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount) => false;

        public IScopeSource ScopeSource => EmptyScopeSource.Instance;
        public CodeType Type => StandardType.Unknown.Instance;
        public IParentElement ParentElement => null;

        public IDisposable AddDependent(IDependent dependent) => Disposable.Empty;

        public void Dispose() { }
    }
}