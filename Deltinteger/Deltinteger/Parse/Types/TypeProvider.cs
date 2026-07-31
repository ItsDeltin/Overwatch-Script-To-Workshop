#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse.Workshop;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse.Types;

class TypeProvider : ICodeTypeInitializer
{
    public static TypeProvider Create(
        string name,
        TypeKind typeKind,
        Func<TypeProviderInitialization, TypeProviderAttributes> typeCreator)
    {

        var typeProvider = new TypeProvider(name, typeKind);
        var attributes = typeCreator(new(name, typeProvider));
        typeProvider.GenericTypes = attributes.AnonymousTypes;
        typeProvider.typeInstanceFactory = attributes.TypeInstanceFactory;
        return typeProvider;
    }

    public static bool IsTypeProviderOfKind(ICodeTypeInitializer typeProvider, TypeKind kind)
    {
        return typeProvider is TypeProvider tp && tp.Kind == kind;
    }

    public delegate TypeInstance TypeInstanceFactory(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker);

    /// <summary>Assists in creating TypeProviders.</summary>
    public sealed class TypeProviderInitialization(string name, TypeProvider typeProvider)
    {
        (ParseInfo parseInfo, DocRange range)? addHoverInformationAt;

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
            parseInfo.Script.Elements.AddDeclarationCall(typeProvider.declarationKey, new(range, true));
            typeProvider.declaredAt = new(parseInfo.Script.Uri, range);
            addHoverInformationAt = (parseInfo, range);
        }

        /// <summary>Checks for any other definitions with the same name as the type.</summary>
        public void CheckForConflict(ParseInfo parseInfo, DocRange range)
        {
            parseInfo.TranslateInfo.CheckConflict(parseInfo, new(name), range);
        }

        /// <summary>Create type documentation from user syntax.</summary>
        public void AddDocumentationFromMetaComment(MetaComment? Doc)
        {
            if (Doc is null) return;

            var parsedMetaComment = ParsedMetaComment.FromMetaComment(Doc);
            typeProvider.documentation = new(parsedMetaComment.Description);
        }

        public void AddMetaFunction(ParseInfo parseInfo, Func<TypeMetaInitialization, TypeMetaAttributes> GetTypeMetaInformation)
        {
            parseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.Meta, () =>
            {
                // If a definition location exists, create a default instance so that
                // we can add hover information. A default instance may be created anyway.
                CodeType? defaultInstance = null;
                if (addHoverInformationAt is not null)
                {
                    defaultInstance = typeProvider.GetInstance();
                    defaultInstance.Call(addHoverInformationAt.Value.parseInfo, addHoverInformationAt.Value.range);
                }

                // Execute provided meta function.
                var metaInformation = GetTypeMetaInformation(new(typeProvider, defaultInstance));

                // Apply received data.
                typeProvider.getAssignerFunction = metaInformation.GetAssignerFunction;
                typeProvider.onInstanceReady = metaInformation.OnInstanceReady;
                typeProvider.OnMetaCompleted();
            });
        }
    }

    /// <summary>`TypeProvider.Create` callers use this while resolving members inside the type.</summary>
    public sealed class TypeMetaInitialization
    {
        readonly TypeProvider typeProvider;
        CodeType? defaultInstance;

        public TypeMetaInitialization(TypeProvider typeProvider, CodeType? defaultInstance)
        {
            this.typeProvider = typeProvider;
            this.defaultInstance = defaultInstance;
        }

        public CodeType GetDefaultInstance()
        {
            defaultInstance ??= typeProvider.GetInstance();
            return defaultInstance;
        }

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

    /// <summary>Contains information about the declared type.
    /// This is returned by the `typeCreator` function given to `TypeProvider.Create`</summary>
    /// <param name="AnonymousTypes">The type arguments declared for this type.</param>
    public readonly record struct TypeProviderAttributes(
        AnonymousType[] AnonymousTypes,
        TypeInstanceFactory TypeInstanceFactory);

    /// <summary>
    /// Data returned by the type factory after the specifics of the type's content is known.
    /// </summary>
    /// <param name="GetAssignerFunction">The function that creates an `IGettableAssigner` used to generate slots for the data type.</param>
    /// <param name="OnInstanceReady">The function that is executed after a `TypeInstance` has completed initialization.</param>
    public readonly record struct TypeMetaAttributes(GetGettableAssigner? GetAssignerFunction, OnInstanceReady OnInstanceReady);

    /// <summary>
    /// A function that creates an `IGettableAssigner` used to generate slots for the data type.
    /// </summary>
    public delegate IGettableAssigner GetGettableAssigner(CodeType type, AssigningAttributes attributes, bool willBeUsedAsArray);

    /// <summary>
    /// A function that is executed after a `TypeInstance` has completed initialization.
    /// </summary>
    public delegate TypeInstanceAttributes OnInstanceReady(CodeType instance, InstanceAnonymousTypeLinker typeLinker);

    /// <summary>A function that is executed to add a type's object variables to the 
    /// index assigner for compilation.</summary>
    public delegate void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, SourceIndexReference source, VarIndexAssigner assigner);

    public readonly record struct TypeInstanceAttributes(
        bool IsStruct,
        int StackLength,
        AddObjectVariablesToAssigner AddObjectVariablesToAssigner);

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
    public TypeKind Kind { get; }

    readonly TypeProviderDeclarationKey declarationKey;
    readonly HashSet<TypeInstance> instances = [];
    readonly TypeElements typeElements = new();
    bool isMetaCompleted;
    TypeInstanceFactory? typeInstanceFactory;
    GetGettableAssigner? getAssignerFunction;
    OnInstanceReady? onInstanceReady;
    MarkupBuilder? documentation;
    Location? declaredAt;

    TypeProvider(string name, TypeKind typeKind)
    {
        Name = name;
        Kind = typeKind;
        declarationKey = new(name);
    }

    public bool BuiltInTypeMatches(Type type) => false;
    public CompletionItem GetCompletion() => new() { Label = Name };
    public CodeType GetInstance() => GetInstanceFromTypeLinker(InstanceAnonymousTypeLinker.Empty);
    public CodeType GetInstance(GetInstanceInfo instanceInfo)
    {
        return GetInstanceFromTypeLinker(new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));
    }
    private TypeInstance GetInstanceFromTypeLinker(InstanceAnonymousTypeLinker typeLinker)
    {
        var existing = FindExistingItemMatchingLinker(typeLinker);
        if (existing is not null) return existing;

        var newInstance = typeInstanceFactory!(this, typeLinker);

        instances.Add(newInstance);
        if (isMetaCompleted)
            newInstance.OnTypeContentReady();

        return newInstance;
    }

    private TypeInstance? FindExistingItemMatchingLinker(InstanceAnonymousTypeLinker typeLinker)
    {
        foreach (var existingInstance in instances)
        {
            var a = existingInstance.Generics;
            var b = typeLinker.SafeTypeArgsFromAnonymousTypes(GenericTypes);

            bool areCompatible = true;

            for (int i = 0; i < a.Length; i++)
                if (!CodeTypeHelpers.AreEqual(a[i], b[i]))
                {
                    areCompatible = false;
                    break;
                }

            if (areCompatible) return existingInstance;
        }
        return null;
    }

    private void OnMetaCompleted()
    {
        if (isMetaCompleted) return;
        isMetaCompleted = true;
        foreach (var instance in instances)
            instance.OnTypeContentReady();
    }

    public class TypeInstance : CodeType, ITypeArrayHandler
    {
        public TypeProvider Provider { get; }
        readonly InstanceAnonymousTypeLinker typeLinker;
        readonly Scope objectScope;
        readonly Scope staticScope;
        readonly Lazy<ArrayFunctionHandler> arrayFunctionHandler;

        bool setupCompleted;
        bool isStruct;
        AddObjectVariablesToAssigner? addObjectVariablesToAssignerFunction;

        public TypeInstance(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker) : base(provider.Name)
        {
            this.Provider = provider;
            this.typeLinker = typeLinker;
            objectScope = new Scope(provider.Name);
            staticScope = new Scope(provider.Name);
            Generics = typeLinker.SafeTypeArgsFromAnonymousTypes(Provider.GenericTypes);
            ArrayHandler = this;
            Description = provider.documentation;
            Kind = provider.Kind;

            arrayFunctionHandler = new(() =>
            {
                ThrowIfSetupNotComplete();
                return isStruct ?
                    new StructInstance.StructArrayFunctionHandler() :
                    new ArrayFunctionHandler();
            });
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
            addObjectVariablesToAssignerFunction?.Invoke(toWorkshop, source, assigner);
        }

        public override IGettableAssigner GetGettableAssigner(AssigningAttributes attributes)
        {
            if (Provider.getAssignerFunction is not null)
                return Provider.getAssignerFunction(this, attributes, false);
            else
                return base.GetGettableAssigner(attributes);
        }

        public void OnTypeContentReady()
        {
            if (setupCompleted) return;
            setupCompleted = true;

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

            var instanceAttributes = Provider.onInstanceReady!.Invoke(this, typeLinker);
            Attributes = new()
            {
                ContainsGenerics = Generics.Any(typeArg => typeArg.Attributes.ContainsGenerics),
                IsStruct = instanceAttributes.IsStruct,
                StackLength = instanceAttributes.StackLength
            };

            isStruct = instanceAttributes.IsStruct;
            addObjectVariablesToAssignerFunction = instanceAttributes.AddObjectVariablesToAssigner;
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            var (linker, didChange) = InstanceAnonymousTypeLinker.ApplyToTypeArguments(
                instanceInfo,
                Provider.GenericTypes,
                Generics);

            return didChange ? Provider.GetInstanceFromTypeLinker(linker) : this;
        }

        public override CompletionItem GetCompletion() => GetTypeCompletion(this);

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.Elements.AddDeclarationCall(Provider.declarationKey, new DeclarationCall(callRange, false));

            if (Provider.declaredAt is not null)
                parseInfo.Script.AddDefinitionLink(callRange, Provider.declaredAt);
        }

        // `ITypeArrayHandler` implementation
        void ITypeArrayHandler.OverrideArray(ArrayType array) { }
        IGettableAssigner ITypeArrayHandler.GetArrayAssigner(AssigningAttributes attributes)
        {
            if (Provider.getAssignerFunction is not null)
                return Provider.getAssignerFunction(this, attributes, true);
            else
                return base.GetGettableAssigner(attributes);
        }
        ArrayFunctionHandler ITypeArrayHandler.GetFunctionHandler() => arrayFunctionHandler.Value;
        // end `ITypeArrayHandler` implementation

        protected void ThrowIfSetupNotComplete()
        {
            if (!setupCompleted)
                throw new Exception("Type information is not ready");
        }
    }

    class TypeProviderDeclarationKey(string name) : IDeclarationKey
    {
        public string Name => name;
    }
}