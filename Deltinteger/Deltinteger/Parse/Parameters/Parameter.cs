using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CodeParameter
    {
        public string Name { get; }
        public CodeType Type { get; }
        public string Documentation { get; }
        public ExpressionOrWorkshopValue DefaultValue { get; }

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

        public CodeParameter(string name, CodeType type, ExpressionOrWorkshopValue defaultValue, string documentation)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            Documentation = documentation;
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

        public virtual object Validate(ScriptFile script, IExpression value, DocRange valueRange) => null;
        public virtual IWorkshopTree Parse(ActionSet actionSet, IExpression expression, bool asElement) => expression.Parse(actionSet, asElement);

        public string GetLabel(bool markdown)
        {
            string type;
            if (Type == null) type = "define";
            else type = Type.Name;

            if (!markdown) return $"{type} {Name}";
            else return $"**{type}** {Name}";
        }

        override public string ToString()
        {
            if (Type == null) return Name;
            else return Type.Name + " " + Name;
        }

        public static ParameterParseResult GetParameters(ParseInfo parseInfo, Scope methodScope, DeltinScriptParser.SetParametersContext context, VariableDefineType defineType = VariableDefineType.Parameter)
        {
            if (context == null) return new ParameterParseResult(new CodeParameter[0], new Var[0]);

            var parameters = new CodeParameter[context.define().Length];
            var vars = new Var[parameters.Length];
            for (int i = 0; i < context.define().Length; i++)
            {
                var newVar = Var.CreateVarFromContext(defineType, parseInfo, context.define(i));
                newVar.Finalize(methodScope);
                vars[i] = newVar;

                ExpressionOrWorkshopValue initialValue = null;
                if (newVar.InitialValue != null) initialValue = new ExpressionOrWorkshopValue(newVar.InitialValue);

                parameters[i] = new CodeParameter(context.define(i).name.Text, newVar.CodeType, initialValue);
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

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            if (Expression != null) return Expression.Parse(actionSet);
            return WorkshopValue;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
    }
}