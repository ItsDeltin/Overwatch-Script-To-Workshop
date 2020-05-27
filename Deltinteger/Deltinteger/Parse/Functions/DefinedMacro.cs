using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }
        private DeltinScriptParser.ExprContext ExpressionToParse { get; }
        private DeltinScriptParser.Define_macroContext context { get; }

        public DefinedMacro(ParseInfo parseInfo, Scope objectScope, Scope staticScope, DeltinScriptParser.Define_macroContext context, CodeType returnType)
            : base(parseInfo, context.name.Text, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            this.context = context;
            Static = context.STATIC() != null;
            SetupScope(Static ? staticScope : objectScope);
            AccessLevel = context.accessor().GetAccessLevel();
            ReturnType = returnType;
            ExpressionToParse = context.expr();
            DoesReturnValue = true;
        }

        public override void SetupParameters()
        {
            SetupParameters(context.setParameters(), false);
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
            if (!containingScope.IsAlreadyInScope(this)) containingScope.AddMethod(this, parseInfo.Script.Diagnostics, DefinedAt.range);
        }

        override public void SetupBlock()
        {
            if (ExpressionToParse != null) Expression = parseInfo.SetCallInfo(CallInfo).GetExpression(methodScope, ExpressionToParse);
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