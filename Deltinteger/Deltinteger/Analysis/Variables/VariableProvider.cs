using DS.Analysis.Types;

namespace DS.Analysis.Variables
{
    class VariableProvider
    {
        public string Name { get; protected set; }
        public ITypeDirector Type { get; protected set; }

        public VariableProvider(string name, ITypeDirector type)
        {
            Name = name;
            Type = type;
        }

        public Variable CreateInstance() => new Variable(this);
    }
}