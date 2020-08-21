using System;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements
{
    public interface ITypeSupplier
    {
        CodeType Default();
        CodeType Any();
        CodeType AnyArray();
        CodeType Boolean();
        CodeType Number();
        CodeType Player();
        CodeType Players();
        CodeType PlayerArray();
        CodeType Vector();
        CodeType PlayerOrVector();
        CodeType Button();
        CodeType ConstButton();
        CodeType EnumType(string typeName);

        public CodeType FromString(string value)
        {
            switch (value)
            {
                case "any": return Any();
                case "any[]": return AnyArray();
                case "boolean": return Boolean();
                case "number": return Number();
                case "player": return Player();
                case "player[]": return PlayerArray();
                case "players": return Players();
                case "vector": return Vector();
                case "player | vector": return PlayerOrVector();
                case "button": return Button();
                case "Button": return ConstButton();
                default: return EnumType(value) ?? throw new NotImplementedException("Type '" + value + "' not handled.");
            }
        }
    }
}