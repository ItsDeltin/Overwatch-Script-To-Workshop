using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;

namespace DS.Analysis.ModuleSystem
{
    class ModuleManager
    {
        public IScopeSource Root => rootSource;


        readonly List<Module> modules = new List<Module>();
        readonly RootModulesSource rootSource;


        public ModuleManager()
        {
            rootSource = new RootModulesSource(this);
        }


        public IDisposable AddModuleSource(IModuleSource origin, IScopeSource scopeSource, string[] modulePath) => ModuleFromPath(modulePath).AddSource(origin, scopeSource);

        public Module ModuleFromPath(string[] path)
        {
            Module current = GetOrAddRootModule(path[0]);

            for (int i = 1; i < path.Length; i++)
                current = current.GetOrAddModuleByName(path[i]);

            return current;
        }

        Module GetOrAddRootModule(string name)
        {
            foreach (var module in modules)
                if (module.Name == name)
                    return module;

            var newModule = new Module(name, null);
            modules.Add(newModule);
            rootSource.Refresh();
            return newModule;
        }

        public IDisposable SubscribeToModuleScope(string[] modulePath, IObserver<ScopeSourceChange> observer) => ModuleFromPath(modulePath).Subscribe(observer);


        /// <summary>Collects root modules into a scope source.</summary>
        class RootModulesSource : IScopeSource
        {
            readonly ObserverCollection<ScopeSourceChange> observers = new ValueObserverCollection<ScopeSourceChange>(ScopeSourceChange.Empty);
            readonly ModuleManager moduleManager;

            public RootModulesSource(ModuleManager moduleManager)
            {
                this.moduleManager = moduleManager;
            }

            public void Refresh()
            {
                var result = moduleManager.modules.Select(module => ScopedElement.CreateType(module.Name, module));
                observers.Set(new ScopeSourceChange(result.ToArray()));
            }

            public IDisposable Subscribe(IObserver<ScopeSourceChange> observer) => observers.Add(observer);
        }
    }
}