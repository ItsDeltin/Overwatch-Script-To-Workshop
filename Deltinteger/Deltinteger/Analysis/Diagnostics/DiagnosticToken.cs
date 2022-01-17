using System;
using System.Reactive.Disposables;
using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Diagnostics
{
    struct DiagnosticToken : IDisposable
    {
        readonly FileDiagnostics fileDiagnostics;
        readonly DocRange range;
        readonly SerialDisposable diagnostic;

        public DiagnosticToken(FileDiagnostics fileDiagnostics, DocRange range)
        {
            this.fileDiagnostics = fileDiagnostics;
            this.range = range;
            diagnostic = new SerialDisposable();
        }

        public IDisposable Error(string message)
        {
            if (range != null)
                return diagnostic.Disposable = fileDiagnostics.Error(message, range);

            return diagnostic.Disposable = System.Reactive.Disposables.Disposable.Empty;
        }

        public void Dispose() => diagnostic.Dispose();
    }
}