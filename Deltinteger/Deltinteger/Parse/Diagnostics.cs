using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class Diagnostics
    {
        private static readonly ConsoleColor[] SeverityColors = new ConsoleColor[] 
        { 
            ConsoleColor.Red,
            ConsoleColor.Yellow,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkGray
        };

        private readonly Dictionary<string, List<Diagnostic>> diagnostics = new Dictionary<string, List<Diagnostic>>();

        public Diagnostics() {}

        public PublishDiagnosticsParams[] GetDiagnostics()
        {
            return diagnostics.Select(diag => 
                new PublishDiagnosticsParams(new Uri(diag.Key).AbsoluteUri, diag.Value.ToArray())
            ).ToArray();
        }

        public bool ContainsErrors()
        {
            return diagnostics.Any(d => d.Value.Any(diag => diag.severity == Diagnostic.Error));
        }

        public void Error(string file, string message, Range range)
        {
            diagnostics[file].Add(new Diagnostic(message, range) { severity = Diagnostic.Error });
        }

        public void Error(string file, SyntaxErrorException ex)
        {
            Error(file, ex.GetInfo(), ex.Range);
        }

        public void Warning(string file, string message, Range range)
        {
            diagnostics[file].Add(new Diagnostic(message, range) { severity = Diagnostic.Warning });
        }

        public void Information(string file, string message, Range range)
        {
            diagnostics[file].Add(new Diagnostic(message, range) { severity = Diagnostic.Information });
        }

        public void Hint(string file, string message, Range range)
        {
            diagnostics[file].Add(new Diagnostic(message, range) { severity = Diagnostic.Hint });
        }

        public void AddDiagnostic(string file, Diagnostic diagnostic)
        {
            diagnostics[file].Add(diagnostic);
        }

        public void PrintDiagnostics(Log log)
        {
            #warning print file
            foreach (var fileDiagnostics in diagnostics.ToArray())
                foreach (var diag in fileDiagnostics.Value.OrderBy(diag => diag.severity))
                    log.Write(LogLevel.Normal, new ColorMod(diag.ToString(), GetDiagnosticColor(diag.severity)));
        }

        public void AddFile(string file)
        {
            diagnostics.Add(file, new List<Diagnostic>());
        }

        private static ConsoleColor GetDiagnosticColor(int errorLevel)
        {
            return SeverityColors[errorLevel - 1];
        }
    }

    public class AdditionalErrorChecking : DeltinScriptBaseVisitor<object>
    {
        private readonly Diagnostics _diagnostics;
        private readonly DeltinScriptParser _parser;
        private readonly string _file;

        public AdditionalErrorChecking(string file, DeltinScriptParser parser, Diagnostics diagnostics)
        {
            _file = file;
            _parser = parser;
            _diagnostics = diagnostics;
        }

        public override object VisitStatement(DeltinScriptParser.StatementContext context)
        {
            switch (context.GetChild(0))
            {
                case DeltinScriptParser.MethodContext _:
                case DeltinScriptParser.DefineContext _:
                case DeltinScriptParser.VarsetContext _:
                case DeltinScriptParser.ExprContext _:
                    if (context.ChildCount == 1)
                        _diagnostics.Error(_file, "Expected ';'", Range.GetRange(context).end.ToRange());
                    break;
            }
            return base.VisitStatement(context);
        }

        public override object VisitCall_parameters(DeltinScriptParser.Call_parametersContext context)
        {
            // Confirm there is an expression after the last ",".
            if (context.children?.Last().GetText() == ",")
                _diagnostics.Error(_file, "Expected parameter.", Range.GetRange(context).end.ToRange());
            return base.VisitCall_parameters(context);
        }

        public override object VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string type  = context.PART(0).GetText();
            string value = context.PART(1)?.GetText();

            if (value == null)
                _diagnostics.Error(_file, "Expected enum value.", Range.GetRange(context));
            
            else if (EnumData.GetEnumValue(type, value) == null)
                _diagnostics.Error(_file, string.Format(SyntaxErrorException.invalidEnumValue, value, type), Range.GetRange(context)); 

            return base.VisitEnum(context);
        }

        public override object VisitRule_if(DeltinScriptParser.Rule_ifContext context)
        {
            if (context.expr() == null)
                _diagnostics.Error(_file, "Expected expression.", Range.GetRange(context));
            return base.VisitRule_if(context);
        }

        public override object VisitVarset(DeltinScriptParser.VarsetContext context)
        {
            if (context.statement_operation() != null && 
                (context.expr().Length == 0 || 
                    context.children.IndexOf(context.expr().Last()) < 
                    context.children.IndexOf(context.statement_operation())
                ))
                _diagnostics.Error(_file, "Expected expression.", Range.GetRange(context));
            return base.VisitVarset(context);
        }

        public override object VisitDefine(DeltinScriptParser.DefineContext context)
        {
            if (context.EQUALS() != null && context.expr() == null)
                _diagnostics.Error(_file, "Expected expression.", Range.GetRange(context));
            return base.VisitDefine(context);
        }
    }

    public class ErrorListener : BaseErrorListener
    {
        private readonly Diagnostics diagnostics;
        private readonly string file;

        public ErrorListener(string file, Diagnostics diagnostics)
        {
            this.diagnostics = diagnostics;
            this.file = file;
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            diagnostics.Error(file, msg, Range.GetRange(offendingSymbol));
        }
    }
}