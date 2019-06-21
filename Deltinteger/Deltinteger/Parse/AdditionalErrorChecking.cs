using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
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
                _diagnostics.Add(new Diagnostic("Expected ';'", Range.GetRange(context)) { severity = Diagnostic.Error });
            return base.VisitStatement(context);
        }

        public override object VisitParameters(DeltinScriptParser.ParametersContext context)
        {
            // Confirm there is an expression after the last ",".
            if (context.children?.Last().GetText() == ",")
                _diagnostics.Add(new Diagnostic("Expected parameter.", Range.GetRange(context)) { severity = Diagnostic.Error });
            return base.VisitParameters(context);
        }

        public override object VisitEnum(DeltinScriptParser.EnumContext context)
        {
            string type  = context.ENUM().GetText();
            string value = context.PART()?.GetText();

            if (value == null)
                _diagnostics.Add(new Diagnostic("Expected enum value.", Range.GetRange(context)) { severity = Diagnostic.Error });
            
            else if (EnumData.GetEnumValue(type, value) == null)
                _diagnostics.Add(new Diagnostic(string.Format(SyntaxErrorException.invalidEnumValue, value, type), Range.GetRange(context)) { severity = Diagnostic.Error }); 

            return base.VisitEnum(context);
        }

        public override object VisitRule_if(DeltinScriptParser.Rule_ifContext context)
        {
            //if (context.expr() == null)
              //  _diagnostics.Add(new Diagnostic("Expected expression.", Range.GetRange(context)) { severity = Diagnostic.Error });
            return base.VisitRule_if(context);
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