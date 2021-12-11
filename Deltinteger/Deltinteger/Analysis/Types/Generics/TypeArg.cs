using DS.Analysis.Scopes;
using DS.Analysis.Expressions.Identifiers;

namespace DS.Analysis.Types.Generics
{
    class TypeArg
    {
        public string Name { get; }
        public bool Single { get; }
        public SingletonCodeTypeProvider DataTypeProvider { get; }
        public ScopedElement ScopedElement { get; }

        public TypeArg(string name, bool single)
        {
            Name = name;
            Single = single;
            DataTypeProvider = new SingletonCodeTypeProvider(name);
            ScopedElement = ScopedElement.Create(name, DataTypeProvider, UnknownIdentifierHandler.Instance, new ProviderPartHandler(DataTypeProvider));
        }
    }
}