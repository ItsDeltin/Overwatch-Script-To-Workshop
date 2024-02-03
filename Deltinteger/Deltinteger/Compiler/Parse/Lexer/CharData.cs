#nullable enable

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

static class CharData
{
    public static readonly char[] IdentifierCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();
    public static readonly char[] NumericalCharacters = "0123456789".ToCharArray();
    public static readonly char[] WhitespaceCharacters = " \t\r\n".ToCharArray();
}