using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }
        private DeltinScriptParser.ExprContext ExpressionToParse { get; }

        public DefinedMacro(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_macroContext context)
            : base(parseInfo, scope, context.name.Text, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            AccessLevel = context.accessor().GetAccessLevel();
            SetupParameters(context.setParameters());

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

            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
        }

        override public void SetupBlock()
        {
            if (ExpressionToParse == null) return;
            Expression = DeltinScript.GetExpression(parseInfo.SetCallInfo(CallInfo), methodScope, ExpressionToParse);
            ReturnType = Expression?.Type();
        }

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            // Assign the parameters.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            for (int i = 0; i < ParameterVars.Length; i++)
                actionSet.IndexAssigner.Add(ParameterVars[i], parameterValues[i]);

            // Parse the expression.
            return Expression.Parse(actionSet);
        }
    }
}