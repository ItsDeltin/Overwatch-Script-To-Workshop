#nullable enable
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

static class VanillaInfo
{
    public static readonly char[] StructureCharacters = new char[] {
        '{', '}', ';', '"', '(', ')', '=', '+', '-', '/', '*', '.', ',', ':', '?', '\r', '\n', '<', '>'
    };

    public static readonly VanillaKeyword GlobalNamespace = VanillaKeyword.EnKwForTesting("Global");
    public static readonly VanillaKeyword GlobalVariableGroup = VanillaKeyword.EnKwForTesting("global");
    public static readonly VanillaKeyword PlayerVariableGroup = VanillaKeyword.EnKwForTesting("player");

    public static readonly VanillaKeyword[] Event = GetConstantKeyword("Event");
    public static readonly VanillaKeyword[] Team = GetConstantKeyword("Team");
    public static readonly VanillaKeyword[] Player = GetConstantKeyword("Player");

    static VanillaKeyword[] GetConstantKeyword(string enumName)
    {
        return ElementRoot.Instance.GetEnum(enumName).Members.Select(m => VanillaKeyword.EnKwForTesting(m.Name)).ToArray();
    }
}