using DS.Analysis.Scopes;

namespace DS.Analysis.Types.Components
{
    /// <summary>A declaration in a type.</summary>
    interface ICodeTypeElement
    {
        ScopedElement ScopedElement { get; }

        public static ICodeTypeElement New(ScopedElement scopedElement) => new CodeTypeElement(scopedElement);

        record CodeTypeElement(ScopedElement ScopedElement) : ICodeTypeElement;
    }

    class ProviderTypeElement : ICodeTypeElement
    {
        public ScopedElement ScopedElement => typeProvider.CreateScopedElement();
        readonly ICodeTypeProvider typeProvider;
        public ProviderTypeElement(ICodeTypeProvider typeProvider) => this.typeProvider = typeProvider;
    }
}