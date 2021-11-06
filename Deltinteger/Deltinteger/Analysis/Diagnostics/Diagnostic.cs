using System;
using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Diagnostics
{
    class Diagnostic : IDisposable
    {
        FileDiagnostics fileDiagnostics;
        string message;
        DocRange range;

        public Diagnostic( FileDiagnostics fileDiagnostics, string message, DocRange range)
        {
            this.fileDiagnostics = fileDiagnostics;
            this.message = message;
            this.range = range;
        }

        public void Dispose() => fileDiagnostics.RemoveDiagnostic(this);
    }
}