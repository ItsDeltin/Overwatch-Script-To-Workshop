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
        CodeType String();
        CodeType Player();
        CodeType Players();
        CodeType PlayerArray();
        CodeType Vector();
        CodeType VectorArray();
        CodeType PlayerOrVector();
        CodeType Button();
        CodeType Hero() => EnumType("Hero");
        CodeType Map() => EnumType("Map");
        CodeType GameMode() => EnumType("GameMode");
        CodeType Team() => EnumType("Team");
        CodeType EnumType(string typeName) => ValueGroupType.GetEnumType(typeName);

        public CodeType FromString(string value)
        {
            switch (value)
            {
                case "any": return Any();
                case "any[]": return AnyArray();
                case "boolean": return Boolean();
                case "number": return Number();
                case "string": return String();
                case "player": return Player();
                case "player[]": return PlayerArray();
                case "players": return Players();
                case "vector": return Vector();
                case "vector[]": return VectorArray();
                case "player | vector": return PlayerOrVector();
                case "button": return Button();
                case "hero": return Hero();
                case "map": return Map();
                case "team": return Team();
                case "gamemode": return GameMode();
                case "color": return EnumType("Color");
                default: return EnumType(value) ?? throw new NotImplementedException("Type '" + value + "' not handled.");
            }
        }
    }
}