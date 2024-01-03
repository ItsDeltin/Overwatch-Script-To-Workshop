#nullable enable

using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse.Vanilla;

class VanillaContext
{
    public void Error(string text, DocRange range) { }
    public void Warning(string text, DocRange range) { }
    public void Info(string text, DocRange range) { }
    public void Hint(string text, DocRange range) { }

    VanillaContext ExpectingType(VanillaType type) => new();
}