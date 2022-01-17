namespace DS.Analysis.Methods
{
    using Types;
    using Types.Components;
    using Scopes;

    class MethodClassElement : ICodeTypeElement
    {
        public ScopedElement ScopedElement => throw new System.NotImplementedException();
        readonly MethodInstance instance;
        public MethodClassElement(MethodInstance instance) => this.instance = instance;
    }
}