using System;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements
{
    public interface ITypeSupplier
    {
        CodeType Default();
        CodeType Any();
        CodeType Boolean();
        CodeType Number();
        CodeType String();
        CodeType Vector();
        CodeType Unknown();
        CodeType Player();
        CodeType EnumType(string typeName);
        CodeType Array(CodeType type) => new ArrayType(this, type);
        CodeType AnyArray() => new ArrayType(this, Any());
        CodeType NumberArray() => new ArrayType(this, Number());
        CodeType VectorArray() => new ArrayType(this, Vector());
        CodeType PlayerArray() => new ArrayType(this, Player());
        CodeType Players() => new PipeType(Player(), PlayerArray());
        CodeType PlayerOrVector() => new PipeType(Player(), Vector());
        CodeType Hero() => EnumType("Hero");
        CodeType Map() => EnumType("Map");
        CodeType GameMode() => EnumType("GameMode");
        CodeType Team() => EnumType("Team");
        CodeType Button() => EnumType("Button");
        CodeType Color() => EnumType("Color");

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
                case "vector | player":
                case "player | vector": return PlayerOrVector();
                case "button": return EnumType("Button");
                case "hero": return Hero();
                case "map": return Map();
                case "team": return Team();
                case "gamemode": return GameMode();
                case "color": return EnumType("Color");
                case "hero[]": return new ArrayType(this, Hero());
                case "string[]": return new ArrayType(this, String());
                case "hero | hero[]": return new PipeType(Hero(), Array(Hero()));
                default: return EnumType(value) ?? throw new NotImplementedException("Type '" + value + "' not handled.");
            }
        }
    }
}