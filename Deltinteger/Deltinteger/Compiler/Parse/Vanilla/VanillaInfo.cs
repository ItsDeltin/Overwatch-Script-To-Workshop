#nullable enable
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

static class VanillaInfo
{
    public static readonly char[] StructureCharacters = new char[] {
        '{', '}', ';', '"', '(', ')', '[', ']', '=', '+', '-', '/', '*', '^', '.', ',', ':', '?', '\r', '\n', '<', '>'
    };

    public static readonly VanillaKeyword GlobalNamespace = VanillaKeyword.EnKwForTesting("Global");
    public static readonly VanillaKeyword GlobalVariableGroup = VanillaKeyword.EnKwForTesting("global");
    public static readonly VanillaKeyword PlayerVariableGroup = VanillaKeyword.EnKwForTesting("player");

    public static readonly VanillaKeyword On = VanillaKeyword.EnKwForTesting("On");
    public static readonly VanillaKeyword Off = VanillaKeyword.EnKwForTesting("Off");
    public static readonly VanillaKeyword Enabled = VanillaKeyword.EnKwForTesting("Enabled");
    public static readonly VanillaKeyword Disabled = VanillaKeyword.EnKwForTesting("Disabled");

    public static readonly VanillaKeyword[] Event = GetConstantKeyword("Event");
    public static readonly VanillaKeyword[] Team = GetConstantKeyword("Team");
    public static readonly VanillaKeyword[] Player = GetConstantKeyword("Player");

    static VanillaKeyword[] GetConstantKeyword(string enumName)
    {
        return ElementRoot.Instance.GetEnum(enumName).Members.Select(m => VanillaKeyword.EnKwForTesting(m.Name)).ToArray();
    }
}