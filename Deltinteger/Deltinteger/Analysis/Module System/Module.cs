using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Core;

namespace DS.Analysis.ModuleSystem
{
    class Module : AnalysisObject, IScopeSource, ITypePartHandler, IParentElement
    {
        public string Name { get; }

        // IScopeSource
        public ScopedElement[] ScopedElements { get; private set; }

        readonly Module parent;
        readonly List<Module> submodules = new List<Module>();

        readonly List<ModuleSource> providers = new List<ModuleSource>();

        int referenceCount = 0;


        public Module(IMaster master, string name, Module parent) : base(master)
        {
            Name = name;
            this.parent = parent;

            GetIdentifier = new GetStructuredIdentifier(Name, null, parent?.GetIdentifier, GetStructuredIdentifier.PredicateSearch(element => element.TypePartHandler == this));

            if (parent != null)
                AddDisposable(parent.AddDependent(Helper.EmptyDependent));
        }

        public override void Dispose()
        {
            base.Dispose();
            parent?.submodules.Remove(this);
            parent?.RemoveReference();
        }

        public override void Update()
        {
            base.Update();

            // Refresh the scope.
            var scopedElements = Enumerable.Empty<ScopedElement>();
            foreach (var provider in providers)
                scopedElements = scopedElements.Concat(provider.Elements);

            ScopedElements = scopedElements.ToArray();
        }

        public IDisposable AddSource(IModuleSource origin, IScopeSource scopeSource) => new ModuleSource(this, origin, scopeSource);


        /// <summary>Gets a submodule, or creates one if it does not exist.</summary>
        /// <param name="name">The name of the module.</param>
        /// <returns>A reference to the existing or newly created module.</returns>
        public Module GetOrAddModuleByName(string name)
        {
            foreach (var submodule in submodules)
                if (name == submodule.Name)
                    return submodule;

            // Sub module does not exist; create it.
            Module newModule = new Module(Master, name, this);
            submodules.Add(newModule);
            return newModule;
        }


        /// <summary>Adds to the reference count of the module.</summary>
        void AddReference() => referenceCount++;

        /// <summary>Subtracts from the reference count of the module.</summary>
        void RemoveReference()
        {
            referenceCount--;

            if (referenceCount < 0)
                throw new Exception("Module referenceCount < 0");

            if (referenceCount == 0)
                Dispose();
        }

        void TryRemove()
        {
            if (submodules.Count == 0)
                return;
        }


        // IScopeSource
        public IDisposable Subscribe(IObserver<ScopeSourceChange> observer) => new ModuleScopeDereferencer(this, observers.Add(observer));


        // ITypePartHandler
        bool ITypePartHandler.Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount)
        {
            if (typeArgCount > 0)
                errorHandler.ModuleHasTypeArgs();
            return true;
        }

        IDisposable ITypePartHandler.Get(IObserver<TypePartResult> observer, ProviderArguments arguments)
        {
            observer.OnNext(new TypePartResult(this, new Scope(this)));
            return System.Reactive.Disposables.Disposable.Empty;
        }


        // IParentElement
        public IGetIdentifier GetIdentifier { get; }


        /// <summary>Watches a scope that makes up the module.</summary>
        class ModuleSource : IDisposable
        {
            public IDisposable ScopeSubscription { get; }
            public ScopedElement[] Elements { get; private set; }

            readonly Module module;

            public ModuleSource(Module module, IModuleSource origin, IScopeSource scopeSource)
            {
                this.module = module;

                module.providers.Add(this);
                module.AddReference();

                ScopeSubscription = scopeSource.Subscribe(value =>
                {
                    Elements = value.Elements;
                    module.RefreshScope();
                });
            }

            public void Dispose()
            {
                // The source is presumptuously no longer valid. 
                ScopeSubscription.Dispose();
                module.providers.Remove(this);
                module.RemoveReference();
            }
        }

        /// <summary>Removes a reference from the module.</summary>
        class ModuleScopeDereferencer : IDisposable
        {
            readonly Module module;
            readonly IDisposable collectionDisposer;

            public ModuleScopeDereferencer(Module module, IDisposable collectionDisposer)
            {
                this.module = module;
                this.collectionDisposer = collectionDisposer;
                module.AddReference();
            }

            public void Dispose()
            {
                collectionDisposer.Dispose();
                module.RemoveReference();
            }
        }
    }
}