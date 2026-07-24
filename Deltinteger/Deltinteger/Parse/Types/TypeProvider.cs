#nullable enable

using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Workshop;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse.Types;

class TypeProvider : ICodeTypeInitializer
{
    public static TypeProvider Create(
        string name,
        Func<TypeProviderInitialization, TypeProviderAttributes> typeCreator)
    {

        var typeProvider = new TypeProvider(name);
        var attributes = typeCreator(new(name, typeProvider));
        typeProvider.GenericTypes = attributes.AnonymousTypes;
        return typeProvider;
    }

    /// <summary>Assists in creating TypeProviders.</summary>
    public sealed class TypeProviderInitialization(string name, TypeProvider typeProvider)
    {
        /// <summary>Get anonymous types from input syntax.</summary>
        public AnonymousType[] GetAnonymousTypesFromContext(ParseInfo parseInfo, List<TypeArgContext>? typeArgContexts)
        {
            if (typeArgContexts is null)
                return [];
            return AnonymousType.GetGenerics(parseInfo, typeArgContexts, typeProvider);
        }

        /// <summary>Adds a declaration to the script.</summary>
        public void AddDeclaration(ParseInfo parseInfo, DocRange range)
        {
            parseInfo.Script.Elements.AddDeclarationCall(new TempDeclarationCall(name), new(range, true));
        }

        public void AddMetaFunction(ParseInfo parseInfo, Func<TypeMetaInitialization, TypeMetaAttributes> GetTypeMetaInformation)
        {
            parseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.Meta, () =>
            {
                var metaInformation = GetTypeMetaInformation(new(typeProvider));
                typeProvider.getAssignerFunction = metaInformation.GetAssignerFunction;
                typeProvider.OnMetaCompleted();
            });
        }

        class TempDeclarationCall(string name) : IDeclarationKey
        {
            public string Name => name;
        }
    }

    /// <summary>`TypeProvider.Create` callers use this while resolving members inside the type.</summary>
    public sealed class TypeMetaInitialization
    {
        readonly TypeProvider typeProvider;

        public TypeMetaInitialization(TypeProvider typeProvider)
        {
            this.typeProvider = typeProvider;
        }

        public CodeType GetDefaultInstance() => typeProvider.GetInstance();

        public void AddVariableToStaticScope(IVariable variable)
        {
            typeProvider.typeElements.StaticVariables.Add(variable);
        }

        public void AddVariableToObjectScope(IVariable variable)
        {
            typeProvider.typeElements.ObjectVariables.Add(variable);
        }

        public void AddMethodToStaticScope(IMethod method)
        {
            typeProvider.typeElements.StaticMethods.Add(method);
        }
    }

    public readonly record struct TypeProviderAttributes(
        AnonymousType[] AnonymousTypes);

    public readonly record struct TypeMetaAttributes(GetGettableAssigner? GetAssignerFunction);

    public delegate IGettableAssigner GetGettableAssigner(CodeType type, AssigningAttributes attributes);

    readonly struct TypeElements()
    {
        public readonly List<IVariable> StaticVariables = [];
        public readonly List<IVariable> ObjectVariables = [];
        public readonly List<IMethod> StaticMethods = [];
    }

    public string Name { get; }
    public AnonymousType[] GenericTypes { get; private set; } = [];
    public int GenericsCount => GenericTypes.Length;

    readonly HashSet<TypeInstance> instances = [];
    readonly TypeElements typeElements = new();
    bool isMetaCompleted;
    GetGettableAssigner? getAssignerFunction;

    public TypeProvider(string name)
    {
        Name = name;
    }

    public bool BuiltInTypeMatches(Type type) => false;
    public CompletionItem GetCompletion() => new() { Label = Name };
    public CodeType GetInstance() => GetInstance(InstanceAnonymousTypeLinker.Empty);
    public CodeType GetInstance(GetInstanceInfo instanceInfo)
    {
        var newInstance = new TypeInstance(this, new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));

        if (isMetaCompleted)
            newInstance.OnMetaCompleted();
        else
            instances.Add(newInstance);

        return newInstance;
    }

    private void OnMetaCompleted()
    {
        isMetaCompleted = true;
        foreach (var instance in instances)
            instance.OnMetaCompleted();
        instances.Clear();
    }

    public sealed class TypeInstance : CodeType
    {
        public TypeProvider Provider { get; }
        readonly InstanceAnonymousTypeLinker typeLinker;
        readonly Scope objectScope;
        readonly Scope staticScope;

        public TypeInstance(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker) : base(provider.Name)
        {
            this.Provider = provider;
            this.typeLinker = typeLinker;
            objectScope = new Scope(provider.Name);
            staticScope = new Scope(provider.Name);
        }

        public override Scope GetObjectScope()
        {
            return objectScope;
        }

        public override Scope ReturningScope()
        {
            return staticScope;
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, SourceIndexReference source, VarIndexAssigner assigner)
        {

        }

        public override IGettableAssigner GetGettableAssigner(AssigningAttributes attributes)
        {
            if (Provider.getAssignerFunction is not null)
                return Provider.getAssignerFunction(this, attributes);
            else
                return base.GetGettableAssigner(attributes);
        }

        public void OnMetaCompleted()
        {
            void AddVariablesToScope(Scope scope, List<IVariable> variables)
            {
                foreach (var variable in variables)
                    scope.AddNativeVariable(variable.GetInstance(this, typeLinker));
            }
            void AddMethodsToScope(Scope scope, List<IMethod> methods)
            {
                foreach (var method in methods)
                    scope.AddNativeMethod(method);
            }
            AddVariablesToScope(objectScope, Provider.typeElements.ObjectVariables);
            AddVariablesToScope(staticScope, Provider.typeElements.StaticVariables);
            AddMethodsToScope(staticScope, Provider.typeElements.StaticMethods);
        }
    }
}