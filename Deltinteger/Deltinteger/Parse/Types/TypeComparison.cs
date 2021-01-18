using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public static class TypeComparison
    {
        public static bool IsAny(ITypeSupplier supplier, CodeType type) => type.Is(supplier.Any());
    }
}