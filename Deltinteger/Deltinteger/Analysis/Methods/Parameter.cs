namespace DS.Analysis.Methods;
using DS.Analysis.Types;
using DS.Analysis.Documentation;

class Parameter
{
    public string Name { get; }
    public Doc Documentation { get; }
    public CodeType Type { get; }

    public Parameter(string name, Doc documentation, CodeType type)
    {
        Name = name;
        Documentation = documentation;
        Type = type;
    }

    public static Parameter New(string name, Doc documentation, CodeType type) => new Parameter(name, documentation, type);
}