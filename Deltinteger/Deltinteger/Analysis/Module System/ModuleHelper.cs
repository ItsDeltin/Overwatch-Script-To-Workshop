namespace DS.Analysis.ModuleSystem;
using System;
using System.Collections.Generic;
using DS.Analysis.Scopes;
using DS.Analysis.Methods;
using DS.Analysis.Types;

class ModuleMaker
{
    readonly List<Method> methods = new List<Method>();
    readonly List<ICodeTypeProvider> types = new List<ICodeTypeProvider>();

    public void AddMethod(Method method) => methods.Add(method);

    public void AddType(ICodeTypeProvider provider) => types.Add(provider);

    IScopeSource CreateScopeSource()
    {
        // Create scope source.
        var elements = new List<ScopedElement>();

        // Add methods
        foreach (var method in methods)
            elements.Add(ScopedElement.CreateMethod(method));

        return new StaticScopeSource(elements.ToArray());
    }

    public static (Module, IDisposable) CreateInternalModule(ModuleManager manager, string path, Action<ModuleMaker> factory)
    {
        var maker = new ModuleMaker();
        factory(maker);

        // Create module
        var newModule = manager.GetOrAddModuleFromPath(PathFromDotPattern(path));

        // Add source
        return (newModule, newModule.AddSource(maker.CreateScopeSource()));
    }

    public static string[] PathFromDotPattern(string path) => path.Split('.');
}