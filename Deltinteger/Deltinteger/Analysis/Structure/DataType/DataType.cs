using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Components;
using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.DataTypes
{
    class DeclaredDataType : ParentedDeclaredElement
    {
        readonly IDataTypeContentProvider contentProvider;
        readonly CodeTypeProvider codeTypeProvider;
        readonly ScopeSource scopeSource;

        public DeclaredDataType(ContextInfo contextInfo, IDataTypeContentProvider contentProvider)
        {
            // Initialize class values.
            this.contentProvider = contentProvider;
            Name = contentProvider.GetName();

            scopeSource = new ScopeSource(); // Create a scope source for this class.
            contextInfo = contextInfo.AddAppendableSource(scopeSource); // Add the source to the context.

            var setup = contentProvider.Setup(contextInfo);
            DeclaredElements = setup.Declarations;
            codeTypeProvider = setup.DataTypeProvider;
        }

        public override ScopedElement MakeScopedElement(ScopedElementParameters parameters)
        {
            string alias = parameters.Alias ?? Name;
            return new ScopedElement(alias, ScopedElementData.Create(alias, codeTypeProvider, null));
        }

        public override void AddToContent(TypeContentBuilder contentBuilder) => new ProviderTypeElement(codeTypeProvider);


        public override void Dispose()
        {
            base.Dispose();
            contentProvider.Dispose();
        }
    }
}