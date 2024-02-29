#nullable enable

namespace Deltin.Deltinteger.Compiler.Parse.Vanilla;

public record struct WorkshopLanguage(string Name)
{
    public static readonly WorkshopLanguage EnUS = new("en-US");
};