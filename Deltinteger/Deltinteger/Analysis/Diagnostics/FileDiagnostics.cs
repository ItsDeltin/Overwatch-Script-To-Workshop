using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using LSPDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using PublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;

namespace DS.Analysis.Diagnostics
{
    class FileDiagnostics
    {
        public string Source { get; }

        readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public FileDiagnostics(string source)
        {
            Source = source;
        }

        public bool RemoveDiagnostic(Diagnostic diagnostic) => diagnostics.Remove(diagnostic);

        public Diagnostic Error(string message, DocRange range)
        {
            var newDiagnostic = new Diagnostic(this, message, range, LSPDiagnosticSeverity.Error);
            diagnostics.Add(newDiagnostic);
            return newDiagnostic;
        }

        public PublishDiagnosticsParams GetLSPPublishParams() => new PublishDiagnosticsParams() {
            Uri = new Uri(Source),
            Diagnostics = diagnostics.Select(diagnostic => diagnostic.ToLSP()).ToArray()
        };
    }
}