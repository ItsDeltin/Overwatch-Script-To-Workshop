using DS.Analysis.Scopes;

namespace DS.Analysis.Types.Components
{
    interface ICodeTypeElement
    {
        ScopedElement ScopedElement { get; }
    }

    class ProviderTypeElement : ICodeTypeElement
    {
        public ScopedElement ScopedElement => typeProvider.CreateScopedElement();
        readonly CodeTypeProvider typeProvider;
        public ProviderTypeElement(CodeTypeProvider typeProvider) => this.typeProvider = typeProvider;
    }
}