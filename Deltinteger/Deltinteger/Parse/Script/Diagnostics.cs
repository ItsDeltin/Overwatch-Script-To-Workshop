using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;

using PublishDiagnosticsParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.PublishDiagnosticsParams;
using LSDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using DiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;

namespace Deltin.Deltinteger.Parse
{
    public class Diagnostics
    {
        public static readonly ConsoleColor[] SeverityColors = new ConsoleColor[]
        {
            ConsoleColor.Red,
            ConsoleColor.Yellow,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkGray
        };

        private readonly List<FileDiagnostics> diagnosticFiles = new List<FileDiagnostics>();

        public Diagnostics() { }

        public bool ContainsErrors()
        {
            return diagnosticFiles.Any(d => d.Diagnostics.Any(diag => diag.severity == Diagnostic.Error));
        }

        public Diagnostic FindFirstError()
        {
            foreach (var diag in Enumerate())
                if (diag.severity == Diagnostic.Error)
                    return diag;
            return null;
        }

        public FileDiagnostics FromUri(Uri uri)
        {
            var fileDiagnostics = diagnosticFiles.FirstOrDefault(file => file.Uri == uri);
            if (fileDiagnostics is null)
            {
                fileDiagnostics = new FileDiagnostics(uri);
                diagnosticFiles.Add(fileDiagnostics);
            }
            return fileDiagnostics;
        }

        public void PrintDiagnostics(Log log)
        {
            foreach (var fileDiagnostics in diagnosticFiles.ToArray())
                foreach (var diag in fileDiagnostics.Diagnostics.OrderBy(diag => diag.severity))
                    log.Write(LogLevel.Normal, new ColorMod(diag.Info(fileDiagnostics.Uri.AbsoluteUri), GetDiagnosticColor(diag.severity)));
        }

        private static ConsoleColor GetDiagnosticColor(int errorLevel)
        {
            return SeverityColors[errorLevel - 1];
        }

        public PublishDiagnosticsParams[] GetPublishDiagnostics()
        {
            var publishDiagnostics = new PublishDiagnosticsParams[diagnosticFiles.Count];
            for (int i = 0; i < publishDiagnostics.Length; i++)
                publishDiagnostics[i] = diagnosticFiles[i].GetDiagnostics();
            return publishDiagnostics;
        }

        public IEnumerable<Diagnostic> GetErrors()
        {
            var diagnostics = Enumerable.Empty<Diagnostic>();

            foreach (var file in diagnosticFiles)
                diagnostics = diagnostics.Concat(file.Diagnostics.Where(d => d.severity == Diagnostic.Error));

            return diagnostics;
        }

        public IEnumerable<Diagnostic> Enumerate()
        {
            foreach (var file in diagnosticFiles)
                foreach (var diagnostic in file.Diagnostics)
                    yield return diagnostic;
        }

        public string OutputDiagnostics()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var file in diagnosticFiles)
                file.OutputDiagnostics(builder);

            return builder.ToString();
        }
    }

    public class FileDiagnostics
    {
        public Uri Uri { get; }
        private List<Diagnostic> _diagnostics { get; } = new List<Diagnostic>();
        public Diagnostic[] Diagnostics { get { return _diagnostics.ToArray(); } }

        public FileDiagnostics(Uri uri)
        {
            Uri = uri;
        }

        public void Error(string message, DocRange range)
        {
            _diagnostics.Add(new Diagnostic(message, range, Diagnostic.Error));
        }

        public void Warning(string message, DocRange range)
        {
            _diagnostics.Add(new Diagnostic(message, range, Diagnostic.Warning));
        }

        public void Information(string message, DocRange range)
        {
            _diagnostics.Add(new Diagnostic(message, range, Diagnostic.Information));
        }

        public void Hint(string message, DocRange range)
        {
            _diagnostics.Add(new Diagnostic(message, range, Diagnostic.Hint));
        }

        public void AddDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public void AddDiagnostics(Diagnostic[] diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }

        public PublishDiagnosticsParams GetDiagnostics()
        {
            LSDiagnostic[] lsDiagnostics = new LSDiagnostic[_diagnostics.Count];
            for (int i = 0; i < lsDiagnostics.Length; i++)
                lsDiagnostics[i] = new LSDiagnostic()
                {
                    Message = _diagnostics[i].message,
                    Range = _diagnostics[i].range ?? null,
                    Severity = (DiagnosticSeverity)_diagnostics[i].severity,
                    Source = _diagnostics[i].source
                };

            return new PublishDiagnosticsParams()
            {
                Uri = Uri,
                Diagnostics = lsDiagnostics
            };
        }

        public void OutputDiagnostics(StringBuilder builder)
        {
            var sorted = Diagnostics.Where(d => d.severity != Diagnostic.Hint).OrderBy(d => d.severity);
            foreach (var diagnostic in sorted)
                builder.AppendLine(diagnostic.Info(Uri.AbsolutePath));
        }
    }
}