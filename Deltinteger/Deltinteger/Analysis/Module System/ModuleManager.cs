using System;
using System.Collections.Generic;
using DS.Analysis.Scopes;

namespace DS.Analysis.ModuleSystem
{
    class ModuleManager
    {
        readonly List<Module> modules = new List<Module>();

        public IDisposable AddModuleSource(IModuleSource origin, IScopeSource scopeSource, string[] modulePath) => ModuleFromPath(modulePath).AddSource(origin, scopeSource);

        Module ModuleFromPath(string[] path)
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
            return newModule;
        }

        public IDisposable SubscribeToModuleScope(string[] modulePath, IObserver<ScopeSourceChange> observer) => ModuleFromPath(modulePath).Subscribe(observer);
    }
}