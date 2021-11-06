using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Diagnostics
{
    class FileDiagnostics
    {
        readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public bool RemoveDiagnostic(Diagnostic diagnostic) => diagnostics.Remove(diagnostic);

        public Diagnostic Error(string message, DocRange range)
        {
            var newDiagnostic = new Diagnostic(this, message, range);
            diagnostics.Add(newDiagnostic);
            return newDiagnostic;
        }
    }
}