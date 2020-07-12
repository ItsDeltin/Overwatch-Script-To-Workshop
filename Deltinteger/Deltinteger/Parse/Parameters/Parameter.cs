using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CodeParameter : IRestrictedCallHandler
    {
        public string Name { get; set; }
        public CodeType Type { get; set; }
        public string Documentation { get; set; }
        public ExpressionOrWorkshopValue DefaultValue { get; set; }
        public List<RestrictedCallType> RestrictedCalls { get; } = new List<RestrictedCallType>();
        public ParameterInvokedInfo Invoked { get; set; } = new ParameterInvokedInfo();

        public CodeParameter(string name)
        {
            Name = name;
        }

        public CodeParameter(string name, CodeType type)
        {
            Name = name;
            Type = type;
        }

        public CodeParameter(string name, CodeType type, ExpressionOrWorkshopValue defaultValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public CodeParameter(string name, string documentation)
        {
            Name = name;
            Documentation = documentation;
        }

        public CodeParameter(string name, string documentation, CodeType type)
        {
            Name = name;
            Type = type;
            Documentation = documentation;
        }

        public CodeParameter(string name, string documentation, ExpressionOrWorkshopValue defaultValue)
        {
            Name = name;
            Documentation = documentation;
            DefaultValue = defaultValue;
        }

        public CodeParameter(string name, string documentation, CodeType type, ExpressionOrWorkshopValue defaultValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            Documentation = documentation;
        }

        public virtual object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            if (Type is Lambda.BaseLambda) ConstantExpressionResolver.Resolve(value, expr => {
                if (expr is Lambda.LambdaAction lambda)
                    Invoked.OnInvoke(new LambdaParameterInvoke(parseInfo, lambda, valueRange));
            });
            return null;
        }
        public virtual IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => expression.Parse(actionSet);

        public string GetLabel(bool markdown)
        {
            string result = (Type == null ? "define" : Type.GetName()) + " " + Name;
            if (DefaultValue != null) result = "[" + result + "]";
            return result;
        }

        override public string ToString()
        {
            if (Type == null) return Name;
            else return Type.GetName() + " " + Name;
        }

        public void RestrictedCall(RestrictedCall restrictedCall)
        {
            if (!RestrictedCalls.Contains(restrictedCall.CallType))
                RestrictedCalls.Add(restrictedCall.CallType);
        }

        public static ParameterParseResult GetParameters(ParseInfo parseInfo, Scope methodScope, DeltinScriptParser.SetParametersContext context, bool subroutineParameter)
        {
            if (context == null) return new ParameterParseResult(new CodeParameter[0], new Var[0]);

            var parameters = new CodeParameter[context.define().Length];
            var vars = new Var[parameters.Length];
            for (int i = 0; i < context.define().Length; i++)
            {
                Var newVar;

                CodeParameter parameter = new CodeParameter(context.define(i).name.Text);

                // Set up the context handler.
                IVarContextHandler contextHandler = new DefineContextHandler(parseInfo.SetRestrictedCallHandler(parameter), context.define(i));

                // Normal parameter
                if (!subroutineParameter)
                    newVar = new ParameterVariable(methodScope, contextHandler, parameter.Invoked);
                // Subroutine parameter.
                else
                    newVar = new SubroutineParameterVariable(methodScope, contextHandler);

                vars[i] = newVar;
                parameter.Type = newVar.CodeType;

                if (newVar.InitialValue != null) parameter.DefaultValue = new ExpressionOrWorkshopValue(newVar.InitialValue);

                parameters[i] = parameter;
            }

            return new ParameterParseResult(parameters, vars);
        }

        public static string GetLabels(CodeParameter[] parameters, bool markdown)
        {
            return "(" + string.Join(", ", parameters.Select(p => p.GetLabel(markdown))) + ")";
        }
    }

    public class ParameterParseResult
    {
        public CodeParameter[] Parameters { get; }
        public Var[] Variables { get; }

        public ParameterParseResult(CodeParameter[] parameters, Var[] parameterVariables)
        {
            Parameters = parameters;
            Variables = parameterVariables;
        }
    }

    public class ParameterInvokedInfo : Lambda.IBridgeInvocable
    {
        public bool Invoked { get; private set; }
        private List<LambdaParameterInvoke> _onInvoke = new List<LambdaParameterInvoke>();

        public void WasInvoked()
        {
            if (Invoked) return;
            Invoked = true;

            foreach (LambdaParameterInvoke onInvoke in _onInvoke)
                onInvoke.Invoked();
        }

        public void OnInvoke(LambdaParameterInvoke lambdaInvoke)
        {
            if (Invoked) lambdaInvoke.Invoked();
            else _onInvoke.Add(lambdaInvoke);
        }
    }

    public class LambdaParameterInvoke
    {
        public ParseInfo ParseInfo { get; }
        public Lambda.LambdaAction Lambda { get; }
        public DocRange CallRange { get; }

        public LambdaParameterInvoke(ParseInfo parseInfo, Lambda.LambdaAction lambda, DocRange callRange)
        {
            ParseInfo = parseInfo;
            Lambda = lambda;
            CallRange = callRange;
        }

        public void Invoked()
        {
            ParseInfo.CurrentCallInfo?.Call(Lambda.RecursiveCallHandler, CallRange);

            // Add restricted calls.
            foreach (RestrictedCall call in Lambda.CallInfo.RestrictedCalls)
                ParseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                    call.CallType,
                    ParseInfo.GetLocation(CallRange),
                    new CallStrategy("The lambda '" + Lambda.GetLabel(false) + "' calls a restricted value of type '" + RestrictedCall.StringFromCallType(call.CallType) + "'.")
                ));
        }
    }

    public class ExpressionOrWorkshopValue : IExpression
    {
        public IExpression Expression { get; }
        public IWorkshopTree WorkshopValue { get; }

        public ExpressionOrWorkshopValue(IExpression expression)
        {
            Expression = expression;
        }
        public ExpressionOrWorkshopValue(IWorkshopTree workshopValue)
        {
            WorkshopValue = workshopValue;
        }
        public ExpressionOrWorkshopValue() {}

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (Expression != null) return Expression.Parse(actionSet);
            return WorkshopValue;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public static bool UseNonnullParameter(IWorkshopTree input) => input != null && input is V_Null == false;
    }
}