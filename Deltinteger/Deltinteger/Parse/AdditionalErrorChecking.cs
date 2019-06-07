using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class AdditionalErrorChecking : DeltinScriptBaseVisitor<object>
    {
        private readonly BaseErrorListener _errorReporter;
        private readonly DeltinScriptParser _parser;

        public AdditionalErrorChecking(DeltinScriptParser parser, BaseErrorListener errorReporter)
        {
            _parser = parser;
            _errorReporter = errorReporter;
        }

        public override object VisitStatement(DeltinScriptParser.StatementContext context)
        {
            if (context.GetChild(0) is DeltinScriptParser.MethodContext &&
                context.ChildCount == 1)
            {
                _errorReporter.SyntaxError(_parser, context.stop, context.stop.Line, context.stop.Column, "Expected ';'.", null);
            }
            return base.VisitStatement(context);
        }

        public override object VisitParameters(DeltinScriptParser.ParametersContext context)
        {
            // Confirm there is an expression after the last ",".
            if (context.children?.Last().GetText() == ",")
            {
                _errorReporter.SyntaxError(_parser, context.stop, context.stop.Line, context.stop.Column, "Missing parameter.", null);
            }
            return base.VisitParameters(context);
        }
    }

        public class ErrorListener : BaseErrorListener
    {
        public readonly List<SyntaxError> Errors = new List<SyntaxError>();

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.Add(new SyntaxError(msg, offendingSymbol.StartIndex, offendingSymbol.StopIndex + 1));
        }
    }

    public class SyntaxError
    {
        public SyntaxError(string message, int start, int stop)
        {
            Message = message;
            Start = start;
            Stop = stop;
        }
        public readonly string Message;
        public readonly int Start;
        public readonly int Stop;
    }
}