using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using SignatureHelp = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelp;
using SignatureInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureInformation;
using ParameterInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ParameterInformation;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public class CodeParameter
    {
        public string Name { get; }
        public CodeType Type { get; }
        public StringOrMarkupContent Documentation { get; }
        public ExpressionOrWorkshopValue DefaultValue { get; }

        public CodeParameter(string name, CodeType type, ExpressionOrWorkshopValue defaultValue)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public CodeParameter(string name, CodeType type, ExpressionOrWorkshopValue defaultValue, StringOrMarkupContent documentation)
        {
            Name = name;
            Type = type;
            Documentation = documentation;
            DefaultValue = defaultValue;
        }

        public CodeParameter(string name, StringOrMarkupContent documentation)
        {
            Name = name;
            Documentation = documentation;
        }

        public virtual object Validate(ScriptFile script, IExpression value, DocRange valueRange) => null;

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

        public static ParameterParseResult GetParameters(ScriptFile script, DeltinScript translateInfo, Scope methodScope, DeltinScriptParser.SetParametersContext context)
        {
            if (context == null) return new ParameterParseResult(new CodeParameter[0], new Var[0]);

            var parameters = new CodeParameter[context.define().Length];
            var vars = new Var[parameters.Length];
            for (int i = 0; i < context.define().Length; i++)
            {
                var newVar = Var.CreateVarFromContext(VariableDefineType.Parameter, script, translateInfo, context.define(i));
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

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (Expression != null) return Expression.Parse(actionSet);
            return WorkshopValue;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
    }

    public class OverloadChooser
    {
        private ScriptFile script { get; }
        private DeltinScript translateInfo { get; }
        private Scope scope { get; }
        private DocRange genericErrorRange { get; }
        public DocRange CallRange { get; }
        private OverloadError ErrorMessages { get; }

        private IParameterCallable[] AllOverloads { get; }
        private List<IParameterCallable> CurrentOptions { get; set; }
        public IParameterCallable Overload { get; private set; }
        public IExpression[] Values { get; private set; }

        private bool _setContext;
        private DeltinScriptParser.Call_parametersContext CallContext;
        private DeltinScriptParser.Picky_parametersContext PickyContext;
        private DocRange[] ParameterErrors;
        public DocRange[] ParameterRanges { get; private set; }

        public object[] AdditionalParameterData { get; private set; }

        private Dictionary<IParameterCallable, List<Diagnostic>> optionDiagnostics;

        public OverloadChooser(IParameterCallable[] overloads, ScriptFile script, DeltinScript translateInfo, Scope scope, DocRange genericErrorRange, DocRange callRange, OverloadError errorMessages)
        {
            AllOverloads = overloads
                .OrderBy(overload => overload.Parameters.Length)
                .ToArray();
            CurrentOptions = AllOverloads.ToList();
            this.script = script;
            this.translateInfo = translateInfo;
            this.scope = scope;
            this.genericErrorRange = genericErrorRange;
            CallRange = callRange;
            this.ErrorMessages = errorMessages;
        }

        public void SetContext(DeltinScriptParser.Call_parametersContext context)
        {
            if (_setContext) throw new Exception("Already set the context for the overload chooser.");
            CallContext = context;
            _setContext = true;

            IExpression[] values = new IExpression[context.expr().Length];
            ParameterErrors = new DocRange[values.Length];
            var parameterRanges = new List<DocRange>();
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = DeltinScript.GetExpression(script, translateInfo, scope, context.expr(i));
                ParameterErrors[i] = DocRange.GetRange(context.expr(i));
                parameterRanges.Add(ParameterErrors[i]);
            }
            
            if (!SetParameterCount(values.Length)) return;
            if (values.Any(v => v == null)) return;

            // Match by value types and parameter types.
            for (int i = 0; i < values.Length; i++)
            foreach (var option in CurrentOptions)
                CompareParameterTypes(values[i].Type(), option, i);
            GetBestOption();

            Values = new IExpression[Overload.Parameters.Length];
            for (int i = 0; i < values.Length; i++)
                Values[i] = values[i];
            
            if (values.Length < Overload.Parameters.Length)
                parameterRanges.Add(new DocRange(
                    DocRange.GetRange(context).end,
                    CallRange.end
                ));
            
            ParameterRanges = parameterRanges.ToArray();

            // Get the missing parameters.
            for (int i = values.Length; i < Values.Length; i++)
                Values[i] = MissingParameter(Overload.Parameters[i]);
            GetAdditionalData();
        }
        public void SetContext(DeltinScriptParser.Picky_parametersContext context)
        {
            if (_setContext) throw new Exception("Already set the context for the overload chooser.");
            PickyContext = context;
            _setContext = true;

            PickyParameter[] parameters = new PickyParameter[context.picky_parameter().Length];
            ParameterErrors = new DocRange[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                string name = context.picky_parameter(i).PART().GetText();
                IExpression expression = null;

                // Get the expression. If it doesn't exist, add a syntax error.
                if (context.picky_parameter(i).expr() != null)
                {
                    expression = DeltinScript.GetExpression(script, translateInfo, scope, context.picky_parameter(i).expr());
                    ParameterErrors[i] = DocRange.GetRange(context.picky_parameter(i).expr());
                }
                else
                    script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context.picky_parameter(i).TERNARY_ELSE()));
                
                var nameRange = DocRange.GetRange(context.picky_parameter(i).PART());

                // Syntax error if the parameter was already set.
                if (parameters.Any(p => p != null && p.Name == name))
                {
                    script.Diagnostics.Error($"The parameter {name} was already set.", nameRange);
                }
                else
                {
                    // Add the parameter.
                    parameters[i] = new PickyParameter(
                        name,
                        expression,
                        DocRange.GetRange(context.picky_parameter(i)),
                        nameRange
                    );
                }
            }

            if (!SetParameterCount(parameters.Length)) return;

            // Match by value types and parameter types.
            foreach (var parameter in parameters)
            foreach (var option in CurrentOptions)
            {
                int index = -1;
                for (int i = 0; i < option.Parameters.Length; i++)
                if (option.Parameters[i].Name == parameter.Name)
                {
                    index = i;
                    break;
                }

                if (index == -1)
                {
                    // Syntax error if the parameter does not exist.
                    optionDiagnostics[option].Add(new Diagnostic(
                        string.Format(ErrorMessages.ParameterDoesntExist, parameter.Name),
                        parameter.NameRange,
                        Diagnostic.Error
                    ));
                }
                // parameter.Value is null if there is no expression.
                // A syntax error for this was already thrown earlier.
                else if (parameter.Value != null)
                {
                    // Check the types.
                    CompareParameterTypes(parameter.Value.Type(), option, index);
                }
            }
            GetBestOption();

            ParameterRanges = new DocRange[Overload.Parameters.Length];
            IExpression[] values = new IExpression[Overload.Parameters.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int parameterIndex = -1;
                for (int p = 0; p < parameters.Length && parameterIndex == -1; p++)
                    if (parameters[p].Name == Overload.Parameters[i].Name)
                        parameterIndex = p;

                if (parameterIndex != -1)
                {
                    values[i] = parameters[parameterIndex].Value;
                    ParameterRanges[i] = parameters[parameterIndex].FullRange;
                }
                else
                    values[i] = MissingParameter(Overload.Parameters[i]);
            }
            Values = values;
            GetAdditionalData();
        }
        public void SetContext()
        {
            if (_setContext) throw new Exception("Already set the context for the overload chooser.");
            _setContext = true;

            if (!SetParameterCount(0)) return;
            GetBestOption();

            Values = new IExpression[Overload.Parameters.Length];
            for (int i = 0; i < Overload.Parameters.Length; i++)
                Values[i] = MissingParameter(Overload.Parameters[i]);
            
            ParameterRanges = new DocRange[] {
                new DocRange(
                    genericErrorRange.end,
                    CallRange.end
                )
            };
            GetAdditionalData();
        }

        private bool SetParameterCount(int numberOfParameters)
        {
            Overload = AllOverloads
                .OrderBy(o => Math.Abs(numberOfParameters - o.Parameters.Length))
                .FirstOrDefault();
            
            CurrentOptions = CurrentOptions
                .Where(o => numberOfParameters <= o.Parameters.Length)
                .ToList();
            
            SetOptionDiagnostics();
            
            if (CurrentOptions.Count == 0)
            {
                script.Diagnostics.Error(
                    string.Format(ErrorMessages.BadParameterCount, numberOfParameters),
                    genericErrorRange
                );
                return false;
            }
            return true;
        }

        private void SetOptionDiagnostics()
        {
            optionDiagnostics = new Dictionary<IParameterCallable, List<Diagnostic>>();
            // Fill methodDiagnostics.
            foreach (var option in CurrentOptions) optionDiagnostics.Add(option, new List<Diagnostic>());
        }

        private void CompareParameterTypes(CodeType valueType, IParameterCallable option, int parameter)
        {
            if (!CodeType.TypeMatches(option.Parameters[parameter].Type, valueType))
            {
                // The parameter type does not match.
                string msg = string.Format("Expected a value of type {0}.", option.Parameters[parameter].Type.Name);
                optionDiagnostics[option].Add(new Diagnostic(msg, ParameterErrors[parameter], Diagnostic.Error));
            }
        }
    
        private void GetBestOption()
        {
            // If there are any methods with no errors, set that as the best option.
            var optionWithNoErrors = optionDiagnostics.FirstOrDefault(o => o.Value.Count == 0).Key;
            if (optionWithNoErrors != null) Overload = optionWithNoErrors;

            // Add the diagnostics of the best option.
            script.Diagnostics.AddDiagnostics(optionDiagnostics[Overload].ToArray());

            script.AddOverloadData(this);
        }
    
        private IExpression MissingParameter(CodeParameter parameter)
        {
            if (parameter.DefaultValue != null) return parameter.DefaultValue;

            // Parameter is missing.
            script.Diagnostics.Error(
                string.Format(ErrorMessages.MissingParameter, parameter.ToString()),
                genericErrorRange
            );
            return null;
        }
    
        private void GetAdditionalData()
        {
            AdditionalParameterData = new object[Overload.Parameters.Length];
            for (int i = 0; i < Overload.Parameters.Length; i++)
                AdditionalParameterData[i] = Overload.Parameters[i].Validate(script, Values[i], ParameterRanges.ElementAtOrDefault(i));
        }

        public SignatureHelp GetSignatureHelp(Pos caretPos)
        {
            int activeParameter = -1;
            if (ParameterRanges != null)
                for (int i = 0; i < ParameterRanges.Length && activeParameter == -1; i++)
                    if (ParameterRanges[i] != null && ParameterRanges[i].IsInside(caretPos))
                        activeParameter = i;
            
            SignatureInformation[] overloads = new SignatureInformation[CurrentOptions.Count];
            for (int i = 0; i < overloads.Length; i++)
            {
                var parameters = new ParameterInformation[CurrentOptions[i].Parameters.Length];
                for (int p = 0; p < parameters.Length; p++)
                    parameters[p] = new ParameterInformation() {
                        Label = CurrentOptions[i].Parameters[p].GetLabel(false),
                        Documentation = CurrentOptions[i].Parameters[p].Documentation
                    };

                overloads[i] = new SignatureInformation() {
                    Label = CurrentOptions[i].GetLabel(false),
                    Parameters = parameters,
                    Documentation = CurrentOptions[i].Documentation
                };
            }

            return new SignatureHelp()
            {
                ActiveParameter = activeParameter,
                ActiveSignature = CurrentOptions.IndexOf(Overload),
                Signatures = overloads
            };
        }
    }

    class PickyParameter
    {
        public string Name { get; }
        public IExpression Value { get; }
        public DocRange FullRange { get; }
        public DocRange NameRange { get; }

        public PickyParameter(string name, IExpression value, DocRange fullRange, DocRange nameRange)
        {
            Name = name;
            Value = value;
            FullRange = fullRange;
            NameRange = nameRange;
        }
    }

    public class OverloadError
    {
        public string BadParameterCount { get; set; }
        public string ParameterDoesntExist { get; set; }
        public string MissingParameter { get; set; }

        public OverloadError(string errorName)
        {
            BadParameterCount    = $"No overloads for the {errorName} has {{0}} parameters.";
            ParameterDoesntExist = $"The parameter '{{0}}' does not exist in the {errorName}.";
            MissingParameter     = $"The '{{0}}' parameter is missing in the {errorName}.";
        }
    }
}