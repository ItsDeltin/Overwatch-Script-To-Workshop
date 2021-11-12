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
        SetupDataType Setup(StructureContext structure);
        void GetMeta(ContextInfo contextInfo);
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
        TypeReference baseReference;
        IDisposable baseSubscription;

        public DataTypeContentProvider(ClassContext syntax)
        {
            this.syntax = syntax;
            name = syntax.Identifier.Text;
        }

        public string GetName() => name;

        public SetupDataType Setup(StructureContext structure)
        {
            // Get the type args.
            var typeArgs = TypeArgCollection.FromSyntax(syntax.Generics);
            typeArgs.AddToScope(structure.ScopeSource);

            return new SetupDataType(
                declarations: StructureUtility.DeclarationsFromSyntax(structure, syntax.Declarations),
                dataTypeProvider: new CodeTypeProvider(name, typeArgs)
            );
        }

        public void GetMeta(ContextInfo contextInfo)
        {
            // Get the type being inherited.
            if (syntax.Inheriting.Count > 0)
            {
                baseReference = TypeFromContext.TypeReferenceFromContext(contextInfo, syntax.Inheriting[0]);

                // Subscribe to the base class.
                baseSubscription = baseReference.Subscribe(type => {
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