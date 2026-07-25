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
        Func<TypeProviderInitialization, TypeProviderAttributes> typeCreator,
        TypeInstanceFactory typeInstanceFactory)
    {

        var typeProvider = new TypeProvider(name, typeInstanceFactory);
        var attributes = typeCreator(new(name, typeProvider));
        typeProvider.GenericTypes = attributes.AnonymousTypes;
        return typeProvider;
    }

    public delegate TypeInstance TypeInstanceFactory(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker);

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
                typeProvider.onInstanceReady = metaInformation.OnInstanceReady;
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

        /// <summary>Adds a static variable to the type.</summary>
        public void AddStaticVariable(IVariable variable)
        {
            typeProvider.typeElements.StaticVariables.Add(variable);
        }

        /// <summary>Adds an object variable to the type.</summary>
        public void AddObjectVariable(IVariable variable)
        {
            typeProvider.typeElements.ObjectVariables.Add(variable);
        }

        /// <summary>Adds a static method to the type.</summary>
        public void AddStaticMethod(InstanceMethodFactory methodFactory)
        {
            typeProvider.typeElements.StaticMethods.Add(methodFactory);
        }

        /// <summary>Adds the type's type arguments to a scope.</summary>
        public void AddTypeArgumentsToScope(Scope scope)
        {
            foreach (var typeArg in typeProvider.GenericTypes)
                scope.AddType(new GenericCodeTypeInitializer(typeArg));
        }
    }

    public readonly record struct TypeProviderAttributes(
        AnonymousType[] AnonymousTypes);

    public readonly record struct TypeMetaAttributes(GetGettableAssigner? GetAssignerFunction, OnInstanceReady? OnInstanceReady);

    public delegate IGettableAssigner GetGettableAssigner(CodeType type, AssigningAttributes attributes);

    public delegate void OnInstanceReady(CodeType instance, InstanceAnonymousTypeLinker typeLinker);

    public delegate IMethod InstanceMethodFactory(InstanceAnonymousTypeLinker typeLinker);

    readonly struct TypeElements()
    {
        public readonly List<IVariable> StaticVariables = [];
        public readonly List<IVariable> ObjectVariables = [];
        public readonly List<InstanceMethodFactory> StaticMethods = [];
    }

    public string Name { get; }
    public AnonymousType[] GenericTypes { get; private set; } = [];
    public int GenericsCount => GenericTypes.Length;

    readonly TypeInstanceFactory typeInstanceFactory;
    readonly HashSet<TypeInstance> instances = [];
    readonly TypeElements typeElements = new();
    bool isMetaCompleted;
    GetGettableAssigner? getAssignerFunction;
    OnInstanceReady? onInstanceReady;

    TypeProvider(string name, TypeInstanceFactory typeInstanceFactory)
    {
        Name = name;
        this.typeInstanceFactory = typeInstanceFactory;
    }

    public bool BuiltInTypeMatches(Type type) => false;
    public CompletionItem GetCompletion() => new() { Label = Name };
    public CodeType GetInstance() => GetInstance(InstanceAnonymousTypeLinker.Empty);
    public CodeType GetInstance(GetInstanceInfo instanceInfo)
    {
        var newInstance = typeInstanceFactory(this, new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));

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

    public class TypeInstance : CodeType
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
            Generics = typeLinker.SafeTypeArgsFromAnonymousTypes(Provider.GenericTypes);
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
            void AddMethodsToScope(Scope scope, List<InstanceMethodFactory> factories)
            {
                foreach (var factory in factories)
                    scope.AddNativeMethod(factory(typeLinker));
            }
            AddVariablesToScope(objectScope, Provider.typeElements.ObjectVariables);
            AddVariablesToScope(staticScope, Provider.typeElements.StaticVariables);
            AddMethodsToScope(staticScope, Provider.typeElements.StaticMethods);

            Provider.onInstanceReady?.Invoke(this, typeLinker);
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            return Provider.typeInstanceFactory(
                Provider,
                InstanceAnonymousTypeLinker.ApplyToTypeArguments(instanceInfo, Provider.GenericTypes, Generics));
        }
    }
}