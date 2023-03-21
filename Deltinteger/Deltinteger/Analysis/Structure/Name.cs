namespace DS.Analysis.Structure;
using Deltin.Deltinteger.Compiler;

struct Name
{
    public string? Value;
    public bool IsValid;

    public Name(string value)
    {
        Value = value;
        IsValid = true;
    }

    // public static Name FromToken(Token token)
    // {
    //     if (token)
    //         return new Name(token.Text);
    //     else
    //         return new Name();
    // }

    public static string? FromToken(Token token) => token?.Text;
}