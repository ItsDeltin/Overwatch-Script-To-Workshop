using System;
using System.Reactive;
using System.Reactive.Disposables;
using DS.Analysis.Utility;
using DS.Analysis.Structure.Utility;
using DS.Analysis.Core;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Types.Generics;
using DS.Analysis.Types.Components;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.DataTypes
{
    class UserContentProvider : IDataTypeContentProvider
    {
        readonly ClassContext syntax;
        readonly string name;


        // For elements that depend on externals
        readonly DependentCollection externalsDependents = new DependentCollection();

        // Searches the scope for this type
        readonly GetStructuredIdentifier.IScopeSearch scopeSearcher;

        // The elements declared in the type.
        AbstractDeclaredElement[] declaredElements;
        // The type's provider
        ICodeTypeProvider typeProvider;
        // The type parameters
        TypeArgCollection typeParams;

        // The type being extended
        IDisposableTypeDirector baseReference;
        // The dependency to the base type
        IDisposable baseSubscription;

        public UserContentProvider(ClassContext syntax)
        {
            this.syntax = syntax;
            name = syntax.Identifier.Text;

            scopeSearcher = GetStructuredIdentifier.PredicateSearch(element => element.TypePartHandler == typeProvider);
        }

        public string GetName() => name;

        public SetupDataType Setup(ContextInfo contextInfo)
        {
            // Get the type args.
            typeParams = TypeArgCollection.FromSyntax(syntax.Generics);
            typeParams.AddToScope(contextInfo.ScopeAppender);

            // Setup externals
            SetupExternals(contextInfo);

            return new SetupDataType(
                declarations: declaredElements = StructureUtility.DeclarationsFromSyntax(contextInfo, syntax.Declarations),
                // Create the provider that generates directors from type arguments.
                dataTypeProvider: typeProvider = Utility2.CreateCodeTypeProvider(
                    name: name,
                    generics: typeParams,
                    getIdentifier: GetStructuredIdentifier.Create(name, typeParams.GetTypeArgInstances(), contextInfo.Parent?.GetIdentifier, scopeSearcher),
                    instanceFactory: arguments =>
                {
                    // Create the type director from the provided arguments.
                    return Utility2.CreateTypeDirector(setType =>
                    {
                        // Update the director's type when a depended external element is updated.
                        return externalsDependents.Add(Utility2.CreateDependent(contextInfo.Analysis, () =>
                        {
                            // Get the content.
                            var contentBuilder = new TypeContentBuilder(new TypeLinker(typeParams, arguments.TypeArgs));
                            contentBuilder.AddAll(declaredElements);

                            // Type comparison
                            var comparison = new DeclaredCodeTypeComparison(baseReference.Type, this, arguments.TypeArgs);

                            // Create the type and notify the observer.
                            setType(CodeType.Create(
                                content: contentBuilder.ToCodeTypeContent(),
                                comparison: comparison,
                                getIdentifier: GetStructuredIdentifier.Create(name, arguments.TypeArgs, arguments.Parent?.GetIdentifier, scopeSearcher)));
                        }));
                    });
                })
            );
        }

        public IDisposable DependOnExternals(IDependent dependent) => externalsDependents.Add(dependent);

        /// <summary>Subscribes to the type being inheritted.</summary>
        void SetupExternals(ContextInfo contextInfo)
        {
            // Get the type being inherited.
            if (syntax.Inheriting.Count > 0)
            {
                baseReference = TypeFromContext.TypeReferenceFromSyntax(contextInfo, syntax.Inheriting[0]);

                // Subscribe to the base class.
                baseSubscription = baseReference.AddDependent(Utility2.CreateDependent(contextInfo.Analysis, externalsDependents.MarkAsStale));
            }
        }

        public void Dispose()
        {
            baseReference?.Dispose();
            baseSubscription?.Dispose();
        }


        class DeclaredCodeTypeComparison : ITypeComparison
        {
            readonly CodeType baseClass;
            readonly int hashcode;


            public DeclaredCodeTypeComparison(CodeType baseClass, object seed, CodeType[] typeArgs)
            {
                this.baseClass = baseClass;

                // Generate a HashCode for the CodeType.
                HashCode typeHash = new HashCode();
                // Add to the hashcode any unchanging object that may represent a unique identifier to the type.
                // The object added here should be a reference to something that is only created once per type.
                typeHash.Add(seed);
                // Add the type args to the hash.
                foreach (var typeArg in typeArgs)
                    typeHash.Add(typeArg.GetHashCode());

                hashcode = typeHash.ToHashCode();
            }


            public bool CanBeAssignedTo(CodeType other) => Implements(other);

            public bool Implements(CodeType other) => Is(other) || (baseClass != null && baseClass.Comparison.Implements(other));

            public bool Is(CodeType other) => hashcode == other.GetHashCode();

            public int GetTypeHashCode() => hashcode;
        }


        /// <summary>The type provider for a declared data type.</summary>
        /*class DeclaredCodeTypeProvider : CodeTypeProvider
        {
            readonly UserContentProvider contentProvider;
            readonly GetStructuredIdentifier.IScopeSearch scopeSearch;

            public DeclaredCodeTypeProvider(UserContentProvider contentProvider, string name, TypeArgCollection typeArgCollection, IGetIdentifier parent) : base(name, typeArgCollection)
            {
                this.contentProvider = contentProvider;

                scopeSearch = GetStructuredIdentifier.PredicateSearch(element => element.TypePartHandler == this);
                GetIdentifier = GetStructuredIdentifier.Create(name, typeArgCollection.GetTypeArgInstances(), parent, scopeSearch);
            }

            public override IDisposable CreateInstance(IObserver<CodeType> observer, ProviderArguments arguments) =>
                // Watch for external components changing (base class)
                contentProvider.externalsWatcher.Add(Observer.Create<TypeExternals>(externals =>
                {
                    // Get the content.
                    var contentBuilder = new TypeContentBuilder(new TypeLinker(Generics, arguments.TypeArgs));
                    contentBuilder.AddAll(contentProvider.DeclaredElements);

                    // Type comparison
                    var comparison = new DeclaredCodeTypeComparison(externals.baseCodeType, contentProvider, arguments.TypeArgs);

                    // Create the type and notify the observer.
                    observer.OnNext(CodeType.Create(contentBuilder.ToCodeTypeContent(), comparison, CreateIGetIdentifier(arguments)));
                }));

            IGetIdentifier CreateIGetIdentifier(ProviderArguments arguments) => new GetStructuredIdentifier(
                defaultName: contentProvider.name,
                typeArgs: arguments.TypeArgs,
                parent: arguments.Parent?.GetIdentifier,
                scopeSearcher: scopeSearch);


            class DeclaredCodeTypeComparison : ITypeComparison
            {
                readonly CodeType baseClass;
                readonly int hashcode;


                public DeclaredCodeTypeComparison(CodeType baseClass, object seed, CodeType[] typeArgs)
                {
                    this.baseClass = baseClass;

                    // Generate a HashCode for the CodeType.
                    HashCode typeHash = new HashCode();
                    // Add to the hashcode any unchanging object that may represent a unique identifier to the type.
                    // The object added here should be a reference to something that is only created once per type.
                    typeHash.Add(seed);
                    // Add the type args to the hash.
                    foreach (var typeArg in typeArgs)
                        typeHash.Add(typeArg.GetHashCode());

                    hashcode = typeHash.ToHashCode();
                }


                public bool CanBeAssignedTo(CodeType other) => Implements(other);

                public bool Implements(CodeType other) => Is(other) || (baseClass != null && baseClass.Comparison.Implements(other));

                public bool Is(CodeType other) => hashcode == other.GetHashCode();

                public int GetTypeHashCode() => hashcode;
            }
        }*/
    }
}