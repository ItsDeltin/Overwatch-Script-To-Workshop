using System;
using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Diagnostics
{
    struct DiagnosticToken
    {
        readonly FileDiagnostics fileDiagnostics;
        readonly DocRange range;

        public DiagnosticToken(FileDiagnostics fileDiagnostics, DocRange range)
        {
            this.fileDiagnostics = fileDiagnostics;
            this.range = range;
        }

        public IDisposable Error(string message) => fileDiagnostics.Error(message, range);
    }
}