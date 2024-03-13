#nullable enable

using System;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements;

static class ElementJsonTypeHelper
{
    public static T? FromString<T>(IElementsJsonTypeSupplier<T> typeSupplier, string? value) => value switch
    {
        "any" => typeSupplier.Any(),
        "any[]" => typeSupplier.AnyArray(),
        "boolean" => typeSupplier.Boolean(),
        "number" => typeSupplier.Number(),
        "string" => typeSupplier.String(),
        "player" => typeSupplier.Player(),
        "player[]" => typeSupplier.PlayerArray(),
        "players" => typeSupplier.Players(),
        "vector" => typeSupplier.Vector(),
        "vector[]" => typeSupplier.VectorArray(),
        "vector | player" or
        "player | vector" => typeSupplier.PlayerOrVector(),
        "button" => typeSupplier.Button(),
        "hero" => typeSupplier.Hero(),
        "map" => typeSupplier.Map(),
        "team" => typeSupplier.Team(),
        "gamemode" => typeSupplier.GameMode(),
        "color" => typeSupplier.Color(),
        "color | team" or
        "team | color" => typeSupplier.ColorOrTeam(),
        "hero[]" => typeSupplier.Array(typeSupplier.Hero()),
        "string[]" => typeSupplier.Array(typeSupplier.String()),
        "hero | hero[]" => typeSupplier.PipeType(typeSupplier.Hero(), typeSupplier.Array(typeSupplier.Hero())),
        null => typeSupplier.Default(),
        _ => typeSupplier.EnumType(value) ?? typeSupplier.Default()
    };
}

public interface IElementsJsonTypeSupplier<T>
{
    T Default();
    T Any();
    T Boolean();
    T Number();
    T String();
    T Vector();
    T Player();
    T? EnumType(string typeName);
    T AnyArray();
    T VectorArray();
    T PlayerArray();
    T Players();
    T PlayerOrVector();
    T Button();
    T Hero();
    T Color();
    T Map();
    T GameMode();
    T Team();
    T ColorOrTeam();
    T Array(T innerType);
    T PipeType(T a, T b);
}

public interface ITypeSupplier : IElementsJsonTypeSupplier<CodeType>
{
    CodeType Unknown();
    CodeType NumberArray() => new ArrayType(this, Number());
    ArrayProvider ArrayProvider();

    public CodeType? FromString(string value) => ElementJsonTypeHelper.FromString(this, value);
}
