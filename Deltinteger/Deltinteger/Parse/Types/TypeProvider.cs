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
        (ParseInfo parseInfo, DocRange range)? typeDeclarationInformation;

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
            typeDeclarationInformation = (parseInfo, range);
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
            bool DoesRecursiveCall()
            {
                List<CodeType> itemsChecked = [];
                bool IsStorageItemUsingSelf(CodeType type)
                {
                    // Was this item already checked?
                    if (itemsChecked.Any(existing => CodeTypeHelpers.AreEqual(existing, type)))
                        return false;

                    itemsChecked.Add(type);

                    if (type is TypeInstance typeInstance)
                    {
                        // The storage item is referencing this provider.
                        // This is a recursive call.
                        if (typeInstance.Provider == typeProvider)
                            return true;

                        // This is another type, check its storage items to ensure that typeProvider does not appear.
                        foreach (var storageItem in typeInstance.Provider.storageList)
                            if (IsStorageItemUsingSelf(storageItem.GetRealType(typeInstance.TypeLinker)))
                                return true;
                    }
                    else if (type is StructInstance structInstance)
                    {
                        // This is another type, check its storage items to ensure that typeProvider does not appear.
                        foreach (var variable in structInstance.Variables)
                            if (IsStorageItemUsingSelf((CodeType)variable.CodeType))
                                return true;
                    }
                    return false;
                }

                foreach (var storageItem in typeProvider.storageList)
                    if (IsStorageItemUsingSelf(storageItem))
                        return true;
                return false;
            }

            parseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.Meta, () =>
            {
                // If a definition location exists, create a default instance so that
                // we can add hover information. A default instance may be created anyway.
                CodeType? defaultInstance = null;
                if (typeDeclarationInformation is not null)
                {
                    defaultInstance = typeProvider.GetInstance();
                    defaultInstance.Call(typeDeclarationInformation.Value.parseInfo, typeDeclarationInformation.Value.range);
                }

                // Execute provided meta function.
                var metaInformation = GetTypeMetaInformation(new(typeProvider, defaultInstance));

                // Apply received data.
                typeProvider.getAssignerFunction = metaInformation.GetAssignerFunction;
                typeProvider.onInstanceReady = metaInformation.OnInstanceReady;
                typeProvider.OnMetaCompleted();
            });

            parseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.PostMeta, () =>
            {
                if (DoesRecursiveCall())
                {
                    typeProvider.recursiveStorage = true;

                    string recursiveError = $"Type '{typeProvider.Name}' calls itself recursively";
                    if (typeDeclarationInformation is not null)
                        parseInfo.Error(recursiveError, typeDeclarationInformation.Value.range);
                    else
                        throw new Exception(recursiveError);
                }
                typeProvider.readyForPostMetaItems = true;
            });
        }
    }

    /// <summary>`TypeProvider.Create` callers use this while resolving members inside the type.</summary>
    public sealed class TypeMetaInitialization(TypeProvider typeProvider, CodeType? defaultInstance)
    {
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

        /// <summary>Adds a type that influences how storage is created for this type provider.</summary>
        public void AddStorageItem(CodeType storageType) => typeProvider.storageList.Add(storageType);
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

    public delegate PostMetaInformation GetPostMetaInformation();

    public readonly record struct PostMetaInformation(int StackLength);

    public readonly record struct TypeInstanceAttributes(
        bool IsStruct,
        bool NeedsArrayOperationProtection,
        AddObjectVariablesToAssigner AddObjectVariablesToAssigner,
        GetPostMetaInformation GetPostMetaInformation);

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

    readonly List<CodeType> storageList = [];
    readonly TypeProviderDeclarationKey declarationKey;
    readonly HashSet<TypeInstance> instances = [];
    readonly TypeElements typeElements = new();
    bool isMetaCompleted;
    TypeInstanceFactory? typeInstanceFactory;
    GetGettableAssigner? getAssignerFunction;
    OnInstanceReady? onInstanceReady;
    MarkupBuilder? documentation;
    Location? declaredAt;
    bool recursiveStorage;
    bool readyForPostMetaItems;

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
            newInstance.OnMetaReady();

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
            instance.OnMetaReady();
    }

    public class TypeInstance : CodeType, ITypeArrayHandler
    {
        public TypeProvider Provider { get; }
        public InstanceAnonymousTypeLinker TypeLinker { get; }
        readonly Scope objectScope;
        readonly Scope staticScope;
        readonly Lazy<ArrayFunctionHandler> arrayFunctionHandler;

        bool isMetaInformationReady;
        bool isStruct;
        AddObjectVariablesToAssigner? addObjectVariablesToAssignerFunction;

        GetPostMetaInformation? getPostMetaInformationFunction;
        bool isPostMetaInformationReady;
        int postMetaStackLength;

        public TypeInstance(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker) : base(provider.Name)
        {
            this.Provider = provider;
            this.TypeLinker = typeLinker;
            objectScope = new Scope(provider.Name);
            staticScope = new Scope(provider.Name);
            Generics = typeLinker.SafeTypeArgsFromAnonymousTypes(Provider.GenericTypes);
            ArrayHandler = this;
            Description = provider.documentation;
            Kind = provider.Kind;

            Attributes = new TypeInstanceAttributes(this);

            arrayFunctionHandler = new(() =>
            {
                ThrowIfMetaInformationNotAvailable();
                return isStruct ?
                    new StructInstance.StructArrayFunctionHandler() :
                    new ArrayFunctionHandler();
            });
        }

        public override Scope GetObjectScope()
        {
            ThrowIfMetaInformationNotAvailable();
            return objectScope;
        }

        public override Scope ReturningScope()
        {
            ThrowIfMetaInformationNotAvailable();
            return staticScope;
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, SourceIndexReference source, VarIndexAssigner assigner)
        {
            ThrowIfMetaInformationNotAvailable();
            addObjectVariablesToAssignerFunction?.Invoke(toWorkshop, source, assigner);
        }

        public override IGettableAssigner GetGettableAssigner(AssigningAttributes attributes)
        {
            ThrowIfMetaInformationNotAvailable();
            if (Provider.getAssignerFunction is not null)
                return Provider.getAssignerFunction(this, attributes, false);
            else
                return base.GetGettableAssigner(attributes);
        }

        public void OnMetaReady()
        {
            if (isMetaInformationReady) return;
            isMetaInformationReady = true;

            void AddVariablesToScope(Scope scope, List<IVariable> variables)
            {
                foreach (var variable in variables)
                    scope.AddNativeVariable(variable.GetInstance(this, TypeLinker));
            }
            void AddMethodsToScope(Scope scope, List<InstanceMethodFactory> factories)
            {
                foreach (var factory in factories)
                    scope.AddNativeMethod(factory(TypeLinker));
            }
            AddVariablesToScope(objectScope, Provider.typeElements.ObjectVariables);
            AddVariablesToScope(staticScope, Provider.typeElements.StaticVariables);
            AddMethodsToScope(staticScope, Provider.typeElements.StaticMethods);

            var instanceAttributes = Provider.onInstanceReady!.Invoke(this, TypeLinker);
            Attributes = new TypeAttributes()
            {
                ContainsGenerics = Generics.Any(typeArg => typeArg.Attributes.ContainsGenerics),
                IsStruct = instanceAttributes.IsStruct,
            };

            // Array operation protection
            // in case of [single Enum].Append(Any)
            NeedsArrayProtection = instanceAttributes.NeedsArrayOperationProtection;

            isStruct = instanceAttributes.IsStruct;
            addObjectVariablesToAssignerFunction = instanceAttributes.AddObjectVariablesToAssigner;

            getPostMetaInformationFunction = instanceAttributes.GetPostMetaInformation;

            // Allow default assignment operator if this is not a parallel type.
            Operations.AddAssignmentOperator();
            Operations.DefaultAssignment = !isStruct;
        }

        private void GetPostMetaInformation()
        {
            // Retrieving inner class data preceeds the content stage.
            ThrowIfMetaInformationNotAvailable();

            // Very unlikely, function would need to be called inbetween the Meta and Content initialization stages.
            // Still a good idea to ensure that we do not try to find stack lengths when the user defines recursive storage!
            if (!Provider.readyForPostMetaItems)
                throw new Exception("Cannot retrieve content information at this stage");

            // At this point, we have ensured that all declared types have had a chance to take a look at their inner content,
            // and that the compiler has reached a point where stack lengths can be resolved.

            // Do nothing if the content was already retrieved.
            if (isPostMetaInformationReady) return;
            isPostMetaInformationReady = true;

            // Check if the user has defined invalid recursive storage.
            // Eg: `struct A { A value; }`
            // The stack length is unresolvable.
            if (Provider.recursiveStorage) return;

            // All protections completed, get the stack length.
            var postMetaInformation = getPostMetaInformationFunction!();
            postMetaStackLength = postMetaInformation.StackLength;
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
            ThrowIfMetaInformationNotAvailable();
            if (Provider.getAssignerFunction is not null)
                return Provider.getAssignerFunction(this, attributes, true);
            else
                return base.GetGettableAssigner(attributes);
        }
        ArrayFunctionHandler ITypeArrayHandler.GetFunctionHandler() => arrayFunctionHandler.Value;
        // end `ITypeArrayHandler` implementation

        protected void ThrowIfMetaInformationNotAvailable()
        {
            if (!isMetaInformationReady)
                throw new Exception("Type information is not ready");
        }

        sealed class TypeInstanceAttributes(TypeInstance typeInstance) : TypeAttributes
        {
            // Type arguments are known immediately, this value can be resolved right away.
            public override bool ContainsGenerics { get; set; } = typeInstance.Generics.Any(typeArg => typeArg.Attributes.ContainsGenerics);

            // The meta function given to `TypeProvider.Create` will need to execute before it is known
            // whether the type is parallel.
            public override bool IsStruct
            {
                get
                {
                    typeInstance.ThrowIfMetaInformationNotAvailable();
                    return typeInstance.isStruct;
                }
            }

            // Can be retrieved after completion of Meta initialization stage.
            // Types must be aware of their content first.
            public override int StackLength
            {
                get
                {
                    typeInstance.GetPostMetaInformation();
                    return typeInstance.postMetaStackLength;
                }
            }
        }
    }

    class TypeProviderDeclarationKey(string name) : IDeclarationKey
    {
        public string Name => name;
    }
}