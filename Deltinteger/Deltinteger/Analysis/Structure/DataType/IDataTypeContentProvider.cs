using System;
using System.Reactive;
using DS.Analysis.Utility;
using DS.Analysis.Structure.Utility;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Types.Generics;
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
        readonly ObserverCollection<TypeExternals> externalsWatcher = new ValueObserverCollection<TypeExternals>();
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

                    // Create the type and notify the observer.
                    observer.OnNext(CodeType.Create(contentBuilder.ToCodeTypeContent()));
                }));
        }
    }
}