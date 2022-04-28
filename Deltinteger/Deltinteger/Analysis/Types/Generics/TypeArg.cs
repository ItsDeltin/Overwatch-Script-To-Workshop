using DS.Analysis.Scopes;
using DS.Analysis.Expressions.Identifiers;

namespace DS.Analysis.Types.Generics
{
    class TypeArg
    {
        public string Name { get; }
        public bool Single { get; }
        public StandardType DataTypeProvider { get; }
        public ScopedElement ScopedElement { get; }

        public TypeArg(string name, bool single)
        {
            Name = name;
            Single = single;
            DataTypeProvider = new StandardType(name);
            ScopedElement = ScopedElement.CreateType(name, DataTypeProvider.Provider);
        }
    }
}