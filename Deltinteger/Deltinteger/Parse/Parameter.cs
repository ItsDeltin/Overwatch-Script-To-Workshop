namespace Deltin.Deltinteger.Parse
{
    public class CodeParameter
    {
        public string Name { get; }
        public CodeType Type { get; }

        public CodeParameter(string name, CodeType type)
        {
            Name = name;
            Type = type;
        }
    }
}