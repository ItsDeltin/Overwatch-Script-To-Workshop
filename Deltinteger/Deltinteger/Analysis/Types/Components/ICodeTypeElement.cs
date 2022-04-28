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
        readonly ICodeTypeProvider typeProvider;
        public ProviderTypeElement(ICodeTypeProvider typeProvider) => this.typeProvider = typeProvider;
    }
}