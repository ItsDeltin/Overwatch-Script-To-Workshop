using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.DataTypes
{
    class DeclaredDataType : ParentedDeclaredElement
    {
        readonly IDataTypeContentProvider contentProvider;
        CodeTypeProvider codeTypeProvider;

        public DeclaredDataType(StructureContext structure, IDataTypeContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
            Name = contentProvider.GetName();
            codeTypeProvider = new CodeTypeProvider(contentProvider.GetName());

            structure.ScopeSource.AddScopedElement(MakeScopedElement(default(ScopedElementParameters)));

            // Create a scope source for this class.
            var scopeSource = new ScopeSource();

            DeclaredElements = contentProvider.GetDeclarations(structure.SetScopeSource(scopeSource));
        }

        public override void GetMeta(ContextInfo contextInfo)
        {
        }

        public override ScopedElement MakeScopedElement(ScopedElementParameters parameters) => new ScopedDataType(this, parameters.Alias);

        class ScopedDataType : ScopedElement
        {
            readonly DeclaredDataType declaredDataType;

            public ScopedDataType(DeclaredDataType declaredDataType, string alias) : base(alias)
            {
                this.declaredDataType = declaredDataType;
            }

            public override CodeTypeProvider GetCodeTypeProvider()
            {
                if (declaredDataType.codeTypeProvider == null)
                    throw new System.Exception();
                
                return declaredDataType.codeTypeProvider;
            }
        }
    }
}