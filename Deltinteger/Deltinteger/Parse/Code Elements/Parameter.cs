namespace Deltin.Deltinteger.Parse
{
    public class Parameter
    {
        public string Name { get; }
        public CodeType Type { get; }

        public Parameter(string name, CodeType type)
        {
            Name = name;
            Type = type;
        }
    }
}