using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Core;

namespace DS.Analysis.ModuleSystem
{
    class Module : IScopeSource, IParentElement
    {
        /// <summary>The name of the module.</summary>
        public string Name { get; }

        // IScopeSource
        public ScopedElement[] Elements { get; private set; }

        public ITypeNodeManager TypePartHandler { get; }

        /// <summary>The parent module.</summary>
        readonly Module parent;
        /// <summary>The submodules.</summary>
        readonly List<Module> submodules = new List<Module>();

        /// <summary>The module scope sources.</summary>
        readonly List<ModuleSource> providers = new List<ModuleSource>();

        readonly DependencyHandler dependencyHandler;


        public Module(IMaster master, string name, Module parent)
        {
            Name = name;
            this.parent = parent;

            dependencyHandler = new DependencyHandler(master, Update);

            GetIdentifier = new GetStructuredIdentifier(Name, null, parent?.GetIdentifier, GetStructuredIdentifier.PredicateSearch(element => element.TypePartHandler == this));

            // The TypePartHandler manages how the module is used in a type path,
            // for example: MyModule.TypeInModule
            TypePartHandler = Utility2.CreateTypePartHandler((errorHandler, typeArgCount) =>
            {
                // Modules cannot be used with type arguments.
                if (typeArgCount > 0)
                    errorHandler.ModuleHasTypeArgs();
                return true;
            }, arguments => new SerialTypePartInfo(this, this));
        }

        // Adds a dependency to the module.
        public IDisposable AddDependent(IDependent dependent)
        {
            // Add the dependency to the dependency handler.
            var removeDependency = dependencyHandler.AddDependent(dependent);

            // The disposable that the caller disposes when they want to remove the dependency.
            return Disposable.Create(() =>
            {
                // Remove the dependency when the caller disposes the disposable.
                removeDependency.Dispose();
                // Try to remove the module in case there are no more references.
                TryComplete();
            });
        }

        void Update(UpdateHelper updateHelper)
        {
            // Refresh the scope.
            var scopedElements = Enumerable.Empty<ScopedElement>();
            foreach (var provider in providers)
                scopedElements = scopedElements.Concat(provider.ScopeSource.Elements);

            Elements = scopedElements.ToArray();
        }

        void TryComplete()
        {
            if (dependencyHandler.HasDependents || submodules.Count > 0)
                return;

            dependencyHandler.Dispose();
            parent?.submodules.Remove(this);
        }

        public IDisposable AddSource(IModuleSource origin, IScopeSource scopeSource)
        {
            var removeDependency = dependencyHandler.DependOn(scopeSource);
            var moduleSource = new ModuleSource(origin, scopeSource);
            providers.Add(moduleSource);

            return Disposable.Create(() =>
            {
                if (providers.Remove(moduleSource))
                {
                    removeDependency.Dispose();
                    TryComplete();
                }
            });
        }


        /// <summary>Gets a submodule, or creates one if it does not exist.</summary>
        /// <param name="name">The name of the module.</param>
        /// <returns>A reference to the existing or newly created module.</returns>
        public Module GetOrAddModuleByName(string name)
        {
            foreach (var submodule in submodules)
                if (name == submodule.Name)
                    return submodule;

            // Sub module does not exist; create it.
            Module newModule = new Module(dependencyHandler.Master, name, this);
            submodules.Add(newModule);
            return newModule;
        }


        // IParentElement
        public IGetIdentifier GetIdentifier { get; }


        /// <summary>Watches a scope that makes up the module.</summary>
        class ModuleSource
        {
            public IModuleSource Origin { get; }
            public IScopeSource ScopeSource { get; }

            public ModuleSource(IModuleSource origin, IScopeSource scopeSource) => (Origin, ScopeSource) = (origin, scopeSource);
        }
    }
}