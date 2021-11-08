using System;
using Deltin.Deltinteger.Compiler;
using LSPDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LSPDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

namespace DS.Analysis.Diagnostics
{
    class Diagnostic : IDisposable
    {
        readonly FileDiagnostics fileDiagnostics;
        readonly string message;
        readonly DocRange range;
        readonly LSPDiagnosticSeverity severity;

        public Diagnostic(FileDiagnostics fileDiagnostics, string message, DocRange range, LSPDiagnosticSeverity severity)
        {
            this.fileDiagnostics = fileDiagnostics;
            this.message = message;
            this.range = range;
            this.severity = severity;
        }

        public void Dispose() => fileDiagnostics.RemoveDiagnostic(this);

        public LSPDiagnostic ToLSP() => new LSPDiagnostic() {
            Message = message,
            Range = range,
            Source = fileDiagnostics.Source,
            Severity = severity
        };

        public static implicit operator LSPDiagnostic(Diagnostic diagnostic) => diagnostic.ToLSP();
    }
}