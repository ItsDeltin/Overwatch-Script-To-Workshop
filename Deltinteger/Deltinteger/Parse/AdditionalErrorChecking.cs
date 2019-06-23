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
            switch (context.GetChild(0))
            {
                case DeltinScriptParser.MethodContext _:
                case DeltinScriptParser.DefineContext _:
                case DeltinScriptParser.VarsetContext _:
                    if (context.ChildCount == 1)
                        _diagnostics.Add(new Diagnostic("Expected ';'", Range.GetRange(context)) { severity = Diagnostic.Error });
                    break;
            }
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
            string type  = context.PART(0).GetText();
            string value = context.PART(1)?.GetText();

            if (value == null)
                _diagnostics.Add(new Diagnostic("Expected enum value.", Range.GetRange(context)) { severity = Diagnostic.Error });
            
            else if (EnumData.GetEnumValue(type, value) == null)
                _diagnostics.Add(new Diagnostic(string.Format(SyntaxErrorException.invalidEnumValue, value, type), Range.GetRange(context)) { severity = Diagnostic.Error }); 

            return base.VisitEnum(context);
        }

        public override object VisitRule_if(DeltinScriptParser.Rule_ifContext context)
        {
            if (context.expr() == null)
                _diagnostics.Add(new Diagnostic("Expected expression.", Range.GetRange(context)) { severity = Diagnostic.Error });
            return base.VisitRule_if(context);
        }

        public override object VisitVarset(DeltinScriptParser.VarsetContext context)
        {
            if (context.statement_operation() != null && 
                (context.expr().Length == 0 || 
                    context.children.IndexOf(context.expr().Last()) < 
                    context.children.IndexOf(context.statement_operation())
                )
            )
                _diagnostics.Add(new Diagnostic("Expected expression.", Range.GetRange(context)) { severity = Diagnostic.Error });
            return base.VisitVarset(context);
        }

        public override object VisitDefine(DeltinScriptParser.DefineContext context)
        {
            if (context.EQUALS() != null && context.expr() == null)
                _diagnostics.Add(new Diagnostic("Expected expression.", Range.GetRange(context)) { severity = Diagnostic.Error });
            return base.VisitDefine(context);
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