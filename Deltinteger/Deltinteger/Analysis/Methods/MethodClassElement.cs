namespace DS.Analysis.Methods
{
    using Types;
    using Types.Components;
    using Scopes;

    class MethodClassElement : ICodeTypeElement
    {
        public ScopedElement ScopedElement { get; }
        readonly MethodInstance instance;
        public MethodClassElement(MethodInstance instance)
        {
            this.instance = instance;
            ScopedElement = ScopedElement.CreateMethod(instance);
        }
    }
}