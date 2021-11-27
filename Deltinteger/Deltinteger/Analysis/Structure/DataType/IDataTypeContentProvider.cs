using System;
using System.Linq;
using System.Reactive;
using DS.Analysis.Utility;
using DS.Analysis.Structure.Utility;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Types.Generics;
using DS.Analysis.Types.Components;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.DataTypes
{
    interface IDataTypeContentProvider : IDisposable
    {
        string GetName();
        SetupDataType Setup(ContextInfo contextInfo);
    }

    struct SetupDataType
    {
        public readonly AbstractDeclaredElement[] Declarations;
        public readonly CodeTypeProvider DataTypeProvider;

        public SetupDataType(AbstractDeclaredElement[] declarations, CodeTypeProvider dataTypeProvider)
        {
            Declarations = declarations;
            DataTypeProvider = dataTypeProvider;
        }
    }


    class DataTypeContentProvider : IDataTypeContentProvider
    {
        readonly ObserverCollection<TypeExternals> externalsWatcher = new ValueObserverCollection<TypeExternals>(new TypeExternals(null));
        readonly ClassContext syntax;
        readonly string name;


        AbstractDeclaredElement[] declaredElements;

        IDisposableTypeDirector baseReference;
        IDisposable baseSubscription;

        public DataTypeContentProvider(ClassContext syntax)
        {
            this.syntax = syntax;
            name = syntax.Identifier.Text;
        }

        public string GetName() => name;

        public SetupDataType Setup(ContextInfo contextInfo)
        {
            // Get the type args.
            var typeArgs = TypeArgCollection.FromSyntax(syntax.Generics);
            typeArgs.AddToScope(contextInfo.ScopeAppender);

            return new SetupDataType(
                declarations: declaredElements = StructureUtility.DeclarationsFromSyntax(contextInfo, syntax.Declarations),
                dataTypeProvider: new DeclaredCodeTypeProvider(this, name, typeArgs)
            );
        }

        void GetBase(ContextInfo contextInfo)
        {
            // Get the type being inherited.
            if (syntax.Inheriting.Count > 0)
            {
                baseReference = TypeFromContext.TypeReferenceFromSyntax(contextInfo, syntax.Inheriting[0]);

                // Subscribe to the base class.
                baseSubscription = baseReference.Subscribe(type =>
                {
                });
            }
        }

        public void Dispose()
        {
            baseReference?.Dispose();
            baseSubscription?.Dispose();
        }

        record TypeExternals(CodeType baseCodeType);


        class DeclaredCodeTypeProvider : CodeTypeProvider
        {
            readonly DataTypeContentProvider contentProvider;

            public DeclaredCodeTypeProvider(DataTypeContentProvider contentProvider, string name, TypeArgCollection typeArgCollection) : base(name, typeArgCollection)
            {
                this.contentProvider = contentProvider;
            }

            public override IDisposable CreateInstance(IObserver<CodeType> observer, params CodeType[] typeArgs) =>
                // Watch for external components changing (base class)
                contentProvider.externalsWatcher.Add(Observer.Create<TypeExternals>(externals =>
                {
                    // Get the content.
                    var contentBuilder = new TypeContentBuilder();
                    contentBuilder.AddAll(contentProvider.declaredElements);

                    // Type comparison
                    var comparison = new DeclaredCodeTypeComparison(externals.baseCodeType, contentProvider, typeArgs);

                    // Create the type and notify the observer.
                    observer.OnNext(CodeType.Create(contentBuilder.ToCodeTypeContent(), comparison, CreateIGetIdentifier(typeArgs)));
                }));

            IGetIdentifier CreateIGetIdentifier(CodeType[] typeArgs) => GetStructuredIdentifier.Create(
                defaultName: contentProvider.name,
                typeArgs: typeArgs,
                parent: null,
                searchName: context => context.Elements.Reverse().FirstOrDefault(element => element.GetCodeTypeProvider() == this)?.Name);


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
}