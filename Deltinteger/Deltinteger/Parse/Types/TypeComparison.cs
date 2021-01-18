using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public static class TypeComparison
    {
        public static bool IsAny(ITypeSupplier supplier, CodeType type) => type.Implements(supplier.Any());

        public static void ExpectNonConstant(ParseInfo parseInfo, DocRange range, CodeType type)
        {
            if (!IsAny(parseInfo.Types, type))
                parseInfo.Script.Diagnostics.Error("Expected a non-constant type", range);
        }
    }
}