using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

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

        private readonly List<FileDiagnostics> diagnostics = new List<FileDiagnostics>();

        public Diagnostics() {}

        public PublishDiagnosticsParams[] GetDiagnostics()
        {
            return diagnostics.Select(diag => 
                new PublishDiagnosticsParams(new Uri(diag.File).AbsoluteUri, diag.Diagnostics.ToArray())
            ).ToArray();
        }

        public bool ContainsErrors()
        {
            return diagnostics.Any(d => d.Diagnostics.Any(diag => diag.severity == Diagnostic.Error));
        }

        public FileDiagnostics FromFile(string file)
        {
            ThrowIfFileIsAlreadyAdded(file);
            
            FileDiagnostics fileDiagnostics = new FileDiagnostics(file);
            diagnostics.Add(fileDiagnostics);
            return fileDiagnostics;
        }

        public void Add(FileDiagnostics fileDiagnostics)
        {
            ThrowIfFileIsAlreadyAdded(fileDiagnostics.File);

            diagnostics.Add(fileDiagnostics);
        }

        private void ThrowIfFileIsAlreadyAdded(string file)
        {
            if (diagnostics.Any(diag => diag.File == file))
                throw new Exception("A diagnostic tree for the file '" + file + "' was already created.");
        }

        public void PrintDiagnostics(Log log)
        {
            foreach (var fileDiagnostics in diagnostics.ToArray())
                foreach (var diag in fileDiagnostics.Diagnostics.OrderBy(diag => diag.severity))
                    log.Write(LogLevel.Normal, new ColorMod(diag.Info(fileDiagnostics.File), GetDiagnosticColor(diag.severity)));
        }

        private static ConsoleColor GetDiagnosticColor(int errorLevel)
        {
            return SeverityColors[errorLevel - 1];
        }
    }

    public class FileDiagnostics
    {
        public string File { get;}
        private List<Diagnostic> _diagnostics { get; } = new List<Diagnostic>();
        public Diagnostic[] Diagnostics { get { return _diagnostics.ToArray(); }}

        public FileDiagnostics(string file)
        {
            File = file;
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
    }

    public class ErrorListener : BaseErrorListener
    {
        private readonly FileDiagnostics diagnostics;

        public ErrorListener(FileDiagnostics diagnostics)
        {
            this.diagnostics = diagnostics;
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            diagnostics.Error(msg, DocRange.GetRange(offendingSymbol));
        }
    }
}