using System;
using System.Linq;
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
        readonly ClassContext syntax;
        readonly string name;
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
                declarations: StructureUtility.DeclarationsFromSyntax(contextInfo, syntax.Declarations),
                dataTypeProvider: new CodeTypeProvider(name, typeArgs)
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
    }
}