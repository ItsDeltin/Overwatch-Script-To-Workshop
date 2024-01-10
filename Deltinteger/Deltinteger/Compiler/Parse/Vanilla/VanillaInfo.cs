#nullable enable
namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

static class VanillaInfo
{
    public static readonly char[] StructureCharacters = new char[] {
        '{', '}', ';', '"', '(', ')', '=', '+', '-', '/', '*', '.', ',', ':', '?', '\r', '\n'
    };

    public static readonly VanillaKeyword GlobalVariableGroup = VanillaKeyword.EnKwForTesting("global");
    public static readonly VanillaKeyword PlayerVariableGroup = VanillaKeyword.EnKwForTesting("player");
}