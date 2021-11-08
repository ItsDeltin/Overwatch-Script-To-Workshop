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

        public override ScopedElement MakeScopedElement(ScopedElementParameters parameters)
        {
            string alias = parameters.Alias ?? Name;
            return new ScopedElement(alias, new ScopedDataTypeData(this, alias));
        }

        class ScopedDataTypeData : ScopedElementData
        {
            readonly DeclaredDataType declaredDataType;
            readonly string alias;

            public ScopedDataTypeData(DeclaredDataType declaredDataType, string alias)
            {
                this.declaredDataType = declaredDataType;
                this.alias = alias;
            }

            public override CodeTypeProvider GetCodeTypeProvider()
            {
                if (declaredDataType.codeTypeProvider == null)
                    throw new System.Exception();
                
                return declaredDataType.codeTypeProvider;
            }

            public override bool IsMatch(string name) => alias == name;
        }
    }
}