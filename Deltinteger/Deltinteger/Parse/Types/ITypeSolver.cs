using ITypeSupplier = Deltin.Deltinteger.Elements.ITypeSupplier;

namespace Deltin.Deltinteger.Parse
{
    public interface ICodeTypeSolver
    {
        CodeType GetCodeType(DeltinScript deltinScript);

        public static string GetNameOrVoid(DeltinScript deltinScript, ICodeTypeSolver solver) => solver == null ? "void" : solver.GetCodeType(deltinScript).GetName();
    }

    class CodeTypeFromStringSolver : ICodeTypeSolver
    {
        private readonly string _typeName;

        public CodeTypeFromStringSolver(string typeName) => _typeName = typeName;
        public CodeType GetCodeType(DeltinScript deltinScript) => ((ITypeSupplier)deltinScript.Types).FromString(_typeName);
    }
}