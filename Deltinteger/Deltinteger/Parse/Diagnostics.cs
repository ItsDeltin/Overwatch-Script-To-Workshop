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

        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public Diagnostics() {}

        public Diagnostic[] GetDiagnostics()
        {
            return diagnostics.ToArray();
        }

        public bool ContainsErrors()
        {
            return diagnostics.Any(d => d.severity == Diagnostic.Error);
        }

        public void Error(string message, Range range)
        {
            diagnostics.Add(new Diagnostic(message, range) { severity = Diagnostic.Error });
        }

        public void Error(SyntaxErrorException ex)
        {
            Error(ex.GetInfo(), ex.Range);
        }

        public void Warning(string message, Range range)
        {
            diagnostics.Add(new Diagnostic(message, range) { severity = Diagnostic.Warning });
        }

        public void Information(string message, Range range)
        {
            diagnostics.Add(new Diagnostic(message, range) { severity = Diagnostic.Information });
        }

        public void Hint(string message, Range range)
        {
            diagnostics.Add(new Diagnostic(message, range) { severity = Diagnostic.Hint });
        }

        public void AddDiagnostic(Diagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
        }

        public void PrintDiagnostics(Log log)
        {
            foreach (var diag in diagnostics.OrderBy(diag => diag.severity))
                log.Write(LogLevel.Normal, new ColorMod(diag.ToString(), GetDiagnosticColor(diag.severity)));
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

        public AdditionalErrorChecking(DeltinScriptParser parser, Diagnostics diagnostics)
        {
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
                        _diagnostics.Error("Expected ';'", Range.GetRange(context).end.ToRange());
                    break;
            }
            return base.VisitStatement(context);
        }

        public override object VisitCall_parameters(DeltinScriptParser.Call_parametersContext context)
        {
            // Confirm there is an expression after the last ",".
            if (context.children?.Last().GetText() == ",")
                _diagnostics.Error("Expected parameter.", Range.GetRange(context).end.ToRange());
            return base.VisitCall_parameters(context);
        }

        public override object VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string type  = context.PART(0).GetText();
            string value = context.PART(1)?.GetText();

            if (value == null)
                _diagnostics.Error("Expected enum value.", Range.GetRange(context));
            
            else if (EnumData.GetEnumValue(type, value) == null)
                _diagnostics.Error(string.Format(SyntaxErrorException.invalidEnumValue, value, type), Range.GetRange(context)); 

            return base.VisitEnum(context);
        }

        public override object VisitRule_if(DeltinScriptParser.Rule_ifContext context)
        {
            if (context.expr() == null)
                _diagnostics.Error("Expected expression.", Range.GetRange(context));
            return base.VisitRule_if(context);
        }

        public override object VisitVarset(DeltinScriptParser.VarsetContext context)
        {
            if (context.statement_operation() != null && 
                (context.expr().Length == 0 || 
                    context.children.IndexOf(context.expr().Last()) < 
                    context.children.IndexOf(context.statement_operation())
                ))
                _diagnostics.Error("Expected expression.", Range.GetRange(context));
            return base.VisitVarset(context);
        }

        public override object VisitDefine(DeltinScriptParser.DefineContext context)
        {
            if (context.EQUALS() != null && context.expr() == null)
                _diagnostics.Error("Expected expression.", Range.GetRange(context));
            return base.VisitDefine(context);
        }
    }

    public class ErrorListener : BaseErrorListener
    {
        private readonly Diagnostics diagnostics;
        public ErrorListener(Diagnostics diagnostics)
        {
            this.diagnostics = diagnostics;
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            diagnostics.Error(msg, Range.GetRange(offendingSymbol));
        }
    }
}