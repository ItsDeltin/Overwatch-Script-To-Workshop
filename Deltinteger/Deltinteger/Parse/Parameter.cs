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

        override public string ToString()
        {
            if (Type == null) return Name;
            else return Type.Name + " " + Name;
        }
    }
}