#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Vanilla;

class StaticAnalysisData
{
    public static readonly StaticAnalysisData Instance = new();

    public VanillaTypeData TypeData { get; } = new();
}

class VanillaTypeData : IElementsJsonTypeSupplier<VanillaType>
{
    public VanillaType UnknownType { get; } = NewType("unknown");
    public VanillaType VoidType { get; } = NewType("void");
    public VanillaType GlobalType { get; } = NewType("global");
    public VanillaType AnyType { get; } = NewType("any", "Custom String", "String");
    public VanillaType BooleanType { get; } = NewType("boolean", "True", "False", "Not");
    public VanillaType NumberType { get; } = NewType("number");
    public VanillaType StringType { get; } = NewType("string", "Custom String", "String");
    public VanillaType VectorType { get; } = NewType("vector", "Vector", "Forward", "Backward", "Left", "Right", "Up", "Down", "Position Of", "Eye Position");
    public VanillaType PlayerType { get; } = NewType("player", "Event Player", "Local Player", "Host Player", "Attacker", "Victim", "Healee", "Healer", "Player Carrying Flag");
    public VanillaType ButtonType { get; } = NewType("button", "Button", "Event Ability");
    public VanillaType HeroType { get; } = NewType("hero", "Hero", "Hero Of", "Hero Being Duplicated");
    public VanillaType ColorType { get; } = NewType("color", "Color", "Custom Color");
    public VanillaType MapType { get; } = NewType("map", "Map", "Current Map");
    public VanillaType GameModeType { get; } = NewType("gamemode", "Game Mode", "Current Game Mode");
    public VanillaType TeamType { get; } = NewType("team", "Team", "Team Of", "Opposite Team Of", "Control Mode Scoring Team");
    public Dictionary<string, VanillaType> EnumTypes { get; }
    readonly List<VanillaArrayType> arrayTypes = new();
    readonly List<VanillaPipeType> pipeTypes = new();

    public VanillaTypeData()
    {
        EnumTypes = ElementRoot.Instance.Enumerators.ToDictionary(
            enumerator => enumerator.Name,
            enumerator => NewType(enumerator.Name)
        );
    }

    static VanillaType NewType(string name, params string[] notableValues) => new(name, notableValues);

    // IElementsJsonTypeSupplier<VanillaType>
    public VanillaType Default() => UnknownType;
    public VanillaType Any() => AnyType;
    public VanillaType Boolean() => BooleanType;
    public VanillaType Number() => NumberType;
    public VanillaType String() => StringType;
    public VanillaType Vector() => VectorType;
    public VanillaType Player() => PlayerType;
    public VanillaType? EnumType(string typeName) => EnumTypes.GetValueOrDefault(typeName);
    public VanillaType AnyArray() => Array(Any());
    public VanillaType VectorArray() => Array(Vector());
    public VanillaType PlayerArray() => Array(Player());
    public VanillaType Players() => PipeType(Player(), Array(Player()));
    public VanillaType PlayerOrVector() => PipeType(Player(), Vector());
    public VanillaType Button() => ButtonType;
    public VanillaType Hero() => HeroType;
    public VanillaType Color() => ColorType;
    public VanillaType Map() => MapType;
    public VanillaType GameMode() => GameModeType;
    public VanillaType Team() => TeamType;
    public VanillaType Array(VanillaType innerType)
    {
        var type = arrayTypes.FirstOrDefault(arrayType => arrayType.InnerType == innerType);

        if (type is null)
        {
            type = new(innerType);

            // Notable values for player array
            if (innerType == PlayerType)
            {
                type.NotableValues = new[] { "All Players" };
            }

            arrayTypes.Add(type);
        }

        return type;
    }
    public VanillaType PipeType(VanillaType a, VanillaType b)
    {
        var type = pipeTypes.FirstOrDefault(pipeType => pipeType.A == a && pipeType.B == b);

        if (type is null)
        {
            type = new(a, b);
            pipeTypes.Add(type);
        }

        return type;
    }
}
