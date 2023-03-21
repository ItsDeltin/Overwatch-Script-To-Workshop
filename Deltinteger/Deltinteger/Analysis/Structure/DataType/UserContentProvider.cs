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
    class UserContentProvider : IDataTypeContentProvider, IParentElement
    {
        readonly ClassContext syntax;
        readonly string? name;

        // Searches the scope for this type
        readonly GetStructuredIdentifier.IScopeSearch scopeSearcher;

        readonly ObserverCollection<int> externalsChanged = new ValueObserverCollection<int>();

        SingleNode node;

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

        // IParentElement
        public IGetIdentifier GetIdentifier { get; private set; }

        public UserContentProvider(ClassContext syntax)
        {
            this.syntax = syntax;
            name = Name.FromToken(syntax.Identifier);

            scopeSearcher = GetStructuredIdentifier.PredicateSearch(element => element.TypePartHandler == typeProvider);
        }

        public string GetName() => name ?? "?";

        public SetupDataType Setup(ContextInfo contextInfo)
        {
            node = contextInfo.Analysis.SingleNode("type externals", () => externalsChanged.Set(0));

            // Get the type args.
            typeParams = TypeArgCollection.FromSyntax(syntax.Generics);
            typeParams.AddToScope(contextInfo.ScopeAppender);

            // Setup externals
            SetupExternals(contextInfo);

            // Assign GetIdentifier
            GetIdentifier = GetStructuredIdentifier.Create(name, typeParams.GetTypeArgInstances(), contextInfo.Parent?.GetIdentifier, scopeSearcher);

            return new SetupDataType(
                declarations: declaredElements = StructureUtility.DeclarationsFromSyntax(contextInfo.SetParent(this), syntax.Declarations),
                // Create the provider that generates directors from type arguments.
                dataTypeProvider: typeProvider = Utility2.CreateProviderAndDirector(
                    name: name,
                    typeParams: typeParams,
                    getIdentifier: GetIdentifier,
                    instanceFactory: helper =>
                {
                    helper.AddDisposable(externalsChanged.Add(_ =>
                    {
                        // Get the content.
                        var contentBuilder = new TypeContentBuilder(new TypeLinker(typeParams, helper.TypeArgs));
                        contentBuilder.AddAll(declaredElements);

                        // Type comparison
                        var comparison = new DeclaredCodeTypeComparison(baseReference?.Type, this, helper.TypeArgs);

                        // Create the type and notify the observer.
                        helper.SetType(CodeType.Create(
                            content: contentBuilder.ToCodeTypeContent(),
                            comparison: comparison,
                            getIdentifier: GetStructuredIdentifier.Create(name, helper.TypeArgs, helper.Parent?.GetIdentifier, scopeSearcher)));
                    }));
                })
            );
        }

        /// <summary>Subscribes to the type being inheritted.</summary>
        void SetupExternals(ContextInfo contextInfo)
        {
            // Get the type being inherited.
            if (syntax.Inheriting.Count > 0)
            {
                baseReference = TypeFromContext.TypeReferenceFromSyntax(contextInfo, syntax.Inheriting[0]);

                // Subscribe to the base class.
                node.DependOn(baseReference);
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
    }
}