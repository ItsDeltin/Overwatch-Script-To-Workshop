using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using DS.Analysis.Core;

namespace DS.Analysis.ModuleSystem
{
    class ModuleManager
    {
        public IScopeSource Root => moduleListScope;


        readonly List<Module> modules = new List<Module>();

        readonly DSAnalysis master;
        // The scope containing the modules.
        readonly SerialScopeSource moduleListScope = new SerialScopeSource();


        public ModuleManager(DSAnalysis master)
        {
            this.master = master;
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
                    // Existing module found
                    return module;

            // Create the module
            var newModule = new Module(master, name, null);
            modules.Add(newModule);

            // Update module scope
            moduleListScope.Elements = modules
                .Select(module => ScopedElement.CreateType(module.Name, module.TypePartHandler))
                .ToArray();

            return newModule;
        }
    }
}