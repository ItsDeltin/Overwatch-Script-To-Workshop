using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.DataTypes
{
    class DeclaredDataType : ParentedDeclaredElement
    {
        readonly IDataTypeContentProvider contentProvider;
        readonly CodeTypeProvider codeTypeProvider;
        readonly ScopeSource scopeSource;

        public DeclaredDataType(StructureContext structure, IDataTypeContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
            Name = contentProvider.GetName();

            // Create a scope source for this class.
            scopeSource = new ScopeSource();

            var setup = contentProvider.Setup(structure.SetScopeSource(scopeSource));
            DeclaredElements = setup.Declarations;
            codeTypeProvider = setup.DataTypeProvider;
        }

        public override void GetMeta(ContextInfo contextInfo)
        {
            contextInfo = contextInfo.AddSource(scopeSource);

            // todo: add anonymous types to scope *then* call base.GetMeta.
            contentProvider.GetMeta(contextInfo);
            base.GetMeta(contextInfo);
        }

        public override ScopedElement MakeScopedElement(ScopedElementParameters parameters)
        {
            string alias = parameters.Alias ?? Name;
            return new ScopedElement(alias, new ScopedDataTypeData(this, alias));
        }

        public override void Dispose()
        {
            base.Dispose();
            contentProvider.Dispose();
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