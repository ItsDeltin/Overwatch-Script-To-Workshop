using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }
        private DeltinScriptParser.ExprContext ExpressionToParse { get; }
        private DeltinScriptParser.Define_macroContext context { get; }

        public DefinedMacro(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_macroContext context)
            : base(parseInfo, scope, context.name.Text, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            this.context = context;
            AccessLevel = context.accessor().GetAccessLevel();
            Static = context.STATIC() != null;

            if (context.TERNARY_ELSE() == null)
            {
                parseInfo.Script.Diagnostics.Error("Expected :", DocRange.GetRange(context).end.ToRange());
            }
            else
            {
                ExpressionToParse = context.expr();
                if (ExpressionToParse == null)
                    parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context.TERNARY_ELSE()));
            }
        }

        public override void SetupParameters()
        {
            SetupParameters(context.setParameters(), false);
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
            containingScope.AddMethod(this, parseInfo.Script.Diagnostics, DefinedAt.range);
        }

        override public void SetupBlock()
        {
            if (ExpressionToParse != null)
            {
                Expression = DeltinScript.GetExpression(parseInfo.SetCallInfo(CallInfo), methodScope, ExpressionToParse);
                ReturnType = Expression?.Type();
            }
            foreach (var listener in listeners) listener.Applied();
        }

        override public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Assign the parameters.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            for (int i = 0; i < ParameterVars.Length; i++)
                actionSet.IndexAssigner.Add(ParameterVars[i], methodCall.ParameterValues[i]);

            // Parse the expression.
            return Expression.Parse(actionSet);
        }
    }
}