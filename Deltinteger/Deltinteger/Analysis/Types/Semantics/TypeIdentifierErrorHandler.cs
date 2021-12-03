using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Types.Semantics
{
    using Diagnostics;

    class TypeIdentifierErrorHandler : ITypeIdentifierErrorHandler
    {
        readonly FileDiagnostics diagnostics;
        readonly string referenceName;
        readonly DocRange range;
        Diagnostic currentDiagnostic;

        public TypeIdentifierErrorHandler(FileDiagnostics diagnostics, string name, DocRange range)
        {
            this.diagnostics = diagnostics;
            this.referenceName = name;
            this.range = range;
        }

        public void Dispose() => currentDiagnostic?.Dispose();

        public void GenericCountMismatch(string typeName, int expected) => SetDiagnostic(Err(Messages.GenericCountMismatch(typeName, 0, expected)));

        public void ModuleHasTypeArgs() => SetDiagnostic(Err(Messages.ModuleHasTypeArgs()));

        public void NoTypesMatchName() => SetDiagnostic(Err(Messages.TypeNameNotFound(referenceName)));

        public void Clear() => SetDiagnostic(null);


        Diagnostic Err(string message) => diagnostics.Error(message, range);

        void SetDiagnostic(Diagnostic newDiagnostic)
        {
            currentDiagnostic?.Dispose();
            currentDiagnostic = newDiagnostic;
        }
    }
}