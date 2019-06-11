using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class AdditionalErrorChecking : DeltinScriptBaseVisitor<object>
    {
        private readonly List<Diagnostic> _diagnostics;
        private readonly DeltinScriptParser _parser;

        public AdditionalErrorChecking(DeltinScriptParser parser, List<Diagnostic> diagnostics)
        {
            _parser = parser;
            _diagnostics = diagnostics;
        }

        public override object VisitStatement(DeltinScriptParser.StatementContext context)
        {
            if (context.GetChild(0) is DeltinScriptParser.MethodContext &&
                context.ChildCount == 1)
            {
                //_errorReporter.SyntaxError(_parser, context.stop, context.stop.Line, context.stop.Column, "Expected ';'.", null);
                _diagnostics.Add(new Diagnostic("Expected ';'", Range.GetRange(context)) { severity = Diagnostic.Error });
            }
            return base.VisitStatement(context);
        }

        public override object VisitParameters(DeltinScriptParser.ParametersContext context)
        {
            // Confirm there is an expression after the last ",".
            if (context.children?.Last().GetText() == ",")
            {
                _diagnostics.Add(new Diagnostic("Missing parameter.", Range.GetRange(context)) { severity = Diagnostic.Error });
            }
            return base.VisitParameters(context);
        }
    }

    public class ErrorListener : BaseErrorListener
    {
        public readonly List<Diagnostic> Errors = new List<Diagnostic>();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.Add(new Diagnostic(msg, Range.GetRange(offendingSymbol)));
        }
    }
}