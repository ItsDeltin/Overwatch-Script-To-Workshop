using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using SignatureHelp = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelp;
using SignatureInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureInformation;
using ParameterInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ParameterInformation;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;
using MarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupContent;
using MarkupKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupKind;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class OverloadChooser
    {
        private ParseInfo parseInfo { get; }
        private Scope scope { get; }
        private Scope getter { get; }
        private DocRange genericErrorRange { get; }
        public DocRange CallRange { get; }
        private OverloadError ErrorMessages { get; }

        private IParameterCallable[] AllOverloads { get; }
        private List<IParameterCallable> CurrentOptions { get; set; }

        public OverloadMatch Match { get; private set; }
        public IParameterCallable Overload { get; private set; }
        public IExpression[] Values { get; private set; }
        public DocRange[] ParameterRanges { get; private set; }
        public object[] AdditionalParameterData { get; private set; }

        public OverloadChooser(IParameterCallable[] overloads, ParseInfo parseInfo, Scope elementScope, Scope getter, DocRange genericErrorRange, DocRange callRange, OverloadError errorMessages)
        {
            AllOverloads = overloads
                .OrderBy(overload => overload.Parameters.Length)
                .ToArray();
            CurrentOptions = AllOverloads.ToList();
            this.parseInfo = parseInfo;
            this.scope = elementScope;
            this.getter = getter;
            this.genericErrorRange = genericErrorRange;
            CallRange = callRange;
            this.ErrorMessages = errorMessages;

            parseInfo.Script.AddOverloadData(this);
        }

        public void Apply(DeltinScriptParser.Call_parametersContext context)
        {
            PickyParameter[] inputParameters = ParametersFromContext(context);

            // Compare parameter counts.
            if (!SetParameterCount(inputParameters.Length)) return;

            // Match overloads.
            OverloadMatch[] matches = new OverloadMatch[CurrentOptions.Count];
            for (int i = 0; i < matches.Length; i++) matches[i] = MatchOverload(CurrentOptions[i], inputParameters, context);

            // Choose the best option.
            Match = BestOption(matches);
            Values = Match.OrderedParameters.Select(op => op?.Value).ToArray();
            ParameterRanges = Match.OrderedParameters.Select(op => op?.ExpressionRange).ToArray();

            GetAdditionalData();
        }

        private PickyParameter[] ParametersFromContext(DeltinScriptParser.Call_parametersContext context)
        {
            // Return empty if context is null.
            if (context == null) return new PickyParameter[0];

            // Create the parameters array with the same length as the number of input parameters.
            PickyParameter[] parameters = new PickyParameter[context.call_parameter().Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                PickyParameter parameter = new PickyParameter(false);
                parameters[i] = parameter;

                // Get the picky name.
                // A is not picky, B is picky.
                // 'name' will be null depending on how the parameter is written. A is null, B is not null.
                // A: '5'
                // B: 'parameter: 5'
                parameter.Name = context.call_parameter(i).PART()?.GetText();
                parameter.Picky = parameter.Name != null;

                if (parameter.Picky) parameter.NameRange = DocRange.GetRange(context.call_parameter(i).PART()); // Get the name range if picky.

                // If the expression context exists, set expression and expressionRange.
                if (context.call_parameter(i).expr() != null)
                {
                    parameter.Value = parseInfo.GetExpression(getter, context.call_parameter(i).expr());
                    parameter.ExpressionRange = DocRange.GetRange(context.call_parameter(i).expr());
                }
                else if (parameter.Picky) // TODO: remove condition so only 'else' remains if parameter-quick-skip is not implemented.
                    // Throw a syntax error if picky.
                    parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context.call_parameter(i).TERNARY_ELSE()));

                // Check if there are any duplicate names.
                if (parameter.Picky && parameters.Any(p => p != null && p.Picky && p != parameter && p.Name == parameter.Name))
                    // If there are, syntax error
                    parseInfo.Script.Diagnostics.Error($"The parameter {parameter.Name} was already set.", parameter.NameRange);
            }

            return parameters;
        }

        private OverloadMatch MatchOverload(IParameterCallable option, PickyParameter[] inputParameters, DeltinScriptParser.Call_parametersContext context)
        {
            PickyParameter lastPicky = null;

            OverloadMatch match = new OverloadMatch(option);
            match.OrderedParameters = new PickyParameter[option.Parameters.Length];

            // Iterate through the option's parameters.
            for (int i = 0; i < inputParameters.Length; i++)
            {
                if (inputParameters[i].ParameterOrdered(option.Parameters[i]))
                {
                    // If the picky parameters end but there is an additional picky parameter, throw a syntax error.
                    if (lastPicky != null && inputParameters[i].Name == null)
                        match.Error($"Named argument '{lastPicky.Name}' is used out-of-position but is followed by an unnamed argument", lastPicky.NameRange);
                    else
                    {
                        match.OrderedParameters[i] = inputParameters[i];
                        // Next contextual parameter
                        if (i == inputParameters.Length - 1 && i < option.Parameters.Length - 1)
                            match.LastContextualParameterIndex = i + 1;
                    }
                }
                else
                {
                    // Picky parameter ends.
                    lastPicky = inputParameters[i];

                    // Set relevant picky parameter.
                    bool nameFound = false;
                    for (int p = 0; p < option.Parameters.Length && !nameFound; p++)
                        if (inputParameters[i].Name == option.Parameters[p].Name)
                        {
                            match.OrderedParameters[p] = inputParameters[i];
                            nameFound = true;
                        }
                    
                    // If the named argument's name is not found, throw an error.
                    if (!nameFound)
                        match.Error($"Named argument '{lastPicky.Name}' does not exist in the function '{option.GetLabel(false)}'.", inputParameters[i].NameRange);
                }
            }

            // Compare parameter types.
            for (int i = 0; i < match.OrderedParameters.Length; i++) match.CompareParameterTypes(i);

            // Get the missing parameters.
            match.GetMissingParameters(genericErrorRange, ErrorMessages, context, CallRange);

            return match;
        }

        private OverloadMatch BestOption(OverloadMatch[] matches)
        {
            // If there are any methods with no errors, set that as the best option.
            OverloadMatch bestOption = matches.FirstOrDefault(match => !match.HasError) ?? matches.FirstOrDefault(match => !match.HasDeterminingError);
            if (bestOption != null) Overload = bestOption.Option;
            else bestOption = matches.First(match => match.Option == Overload);

            // Add the diagnostics of the best option.
            bestOption.AddDiagnostics(parseInfo.Script.Diagnostics);
            CheckAccessLevel();

            return bestOption;
        }

        private void CheckAccessLevel()
        {
            bool accessable = true;

            if (Overload is IMethod asMethod)
            {
                if (!getter.AccessorMatches(asMethod)) accessable = false;
            }
            else if (!getter.AccessorMatches(scope, Overload.AccessLevel)) accessable = false;

            if (!accessable)
                parseInfo.Script.Diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", Overload.GetLabel(false)), genericErrorRange);
        }

        private bool SetParameterCount(int numberOfParameters)
        {
            Overload = AllOverloads
                .OrderBy(o => Math.Abs(numberOfParameters - o.Parameters.Length))
                .FirstOrDefault();
            
            CurrentOptions = CurrentOptions
                .Where(o => numberOfParameters <= o.Parameters.Length)
                .ToList();
                        
            if (CurrentOptions.Count == 0)
            {
                parseInfo.Script.Diagnostics.Error(
                    string.Format(ErrorMessages.BadParameterCount, numberOfParameters),
                    genericErrorRange
                );
                return false;
            }
            return true;
        }
    
        private void GetAdditionalData()
        {
            AdditionalParameterData = new object[Overload.Parameters.Length];
            for (int i = 0; i < Overload.Parameters.Length; i++)
                AdditionalParameterData[i] = Overload.Parameters[i].Validate(parseInfo, Values[i], ParameterRanges.ElementAtOrDefault(i));
        }

        public SignatureHelp GetSignatureHelp(Pos caretPos)
        {
            // Get the active parameter.
            int activeParameter = -1;
            if (ParameterRanges != null)
                // Loop through parameter ranges while activeParameter is -1.
                for (int i = 0; i < ParameterRanges.Length && activeParameter == -1; i++)
                    // If the proved caret position is inside the parameter range, set it as the active parameter.
                    if (ParameterRanges[i] != null && ParameterRanges[i].IsInside(caretPos))
                        activeParameter = i;
            
            // Get the signature information.
            SignatureInformation[] overloads = new SignatureInformation[AllOverloads.Length];
            for (int i = 0; i < overloads.Length; i++)
            {
                // Get the parameter information for the signature.
                var parameters = new ParameterInformation[AllOverloads[i].Parameters.Length];

                // Convert parameters to parameter information.
                for (int p = 0; p < parameters.Length; p++)
                    parameters[p] = new ParameterInformation() {
                        // Get the label to show in the signature.
                        Label = AllOverloads[i].Parameters[p].GetLabel(false),
                        // Get the documentation.
                        Documentation = Extras.GetMarkupContent(AllOverloads[i].Parameters[p].Documentation)
                    };

                // Create the signature information.
                overloads[i] = new SignatureInformation() {
                    Label = AllOverloads[i].GetLabel(false),
                    Parameters = parameters,
                    Documentation = AllOverloads[i].Documentation
                };
            }

            return new SignatureHelp()
            {
                ActiveParameter = activeParameter,
                ActiveSignature = Array.IndexOf(AllOverloads, Overload),
                Signatures = overloads
            };
        }
    }

    public class PickyParameter
    {
        /// <summary>The name of the picky parameter. This will be null if `Picky` is false.</summary>
        public string Name { get; set; }
        /// <summary>The parameter's expression. This will only be null if there is a syntax error.</summary>
        public IExpression Value { get; set; }
        /// <summary>The range of the picky parameter's name. This will be null if `Picky` is false.</summary>
        public DocRange NameRange { get; set; }
        /// <summary>The range of the expression. This will equal `FullRange` if `Picky` is false.</summary>
        public DocRange ExpressionRange { get; set; }
        /// <summary>Determines if the parameter's name is specified.</summary>
        public bool Picky { get; set; }
        /// <summary>If `Prefilled` is true, this parameter was filled by a default value.</summary>
        public bool Prefilled { get; set; }

        public PickyParameter(bool prefilled)
        {
            Prefilled = prefilled;
        }

        public bool ParameterOrdered(CodeParameter parameter) => !Picky || parameter.Name == Name;
    }

    public class OverloadMatch
    {
        public IParameterCallable Option { get; }
        public PickyParameter[] OrderedParameters { get; set; }
        public List<OverloadMatchError> Errors { get; } = new List<OverloadMatchError>();
        public bool HasDeterminingError => Errors.Any(error => error.Vital);
        public bool HasError => Errors.Count > 0;
        public int LastContextualParameterIndex { get; set; } = -1;

        public OverloadMatch(IParameterCallable option)
        {
            Option = option;
        }

        public void Error(string message, DocRange range, bool vital = true) => Errors.Add(new OverloadMatchError(message, range, vital));

        /// <summary>Confirms that a parameter type matches.</summary>
        public void CompareParameterTypes(int parameter)
        {
            CodeType parameterType = Option.Parameters[parameter].Type;
            IExpression value = OrderedParameters[parameter]?.Value;
            if (value == null) return;
            DocRange errorRange = OrderedParameters[parameter].ExpressionRange;

            if (parameterType != null && ((parameterType.IsConstant() && value.Type() == null) || (value.Type() != null && !value.Type().Implements(parameterType))))
            {
                // The parameter type does not match.
                string msg = string.Format("Expected a value of type {0}.", Option.Parameters[parameter].Type.GetName());
                Error(msg, errorRange);
            }
            else if (value.Type() != null && parameterType == null && value.Type().IsConstant())
            {
                string msg = string.Format($"The type '{value.Type().Name}' cannot be used here.");
                Error(msg, errorRange);
            }
        }

        /// <summary>Determines if there are any missing parameters.</summary>
        public void GetMissingParameters(DocRange genericErrorRange, OverloadError messageHandler, DeltinScriptParser.Call_parametersContext context, DocRange functionCallRange)
        {
            for (int i = 0; i < OrderedParameters.Length; i++)
                if (OrderedParameters[i]?.Value == null)
                {
                    if (OrderedParameters[i] == null) OrderedParameters[i] = new PickyParameter(true);
                    AddContextualParameter(context, functionCallRange, i);

                    // Default value
                    if (Option.Parameters[i].DefaultValue != null)
                        // Set the default value.
                        OrderedParameters[i].Value = Option.Parameters[i].DefaultValue;
                    else
                        // Parameter is missing.
                        Error(string.Format(messageHandler.MissingParameter, Option.Parameters[i].Name), genericErrorRange);
                }
        }

        private void AddContextualParameter(DeltinScriptParser.Call_parametersContext context, DocRange functionCallRange, int parameter)
        {
            // No parameters set, set range for first parameter to callRange.
            if (parameter == 0 && OrderedParameters.All(p => p?.Value == null))
            {
                OrderedParameters[0].ExpressionRange = functionCallRange;
            }
            // If this is the last contextual parameter and the context contains comma, set the expression range so signature help works with the last comma when there is no set expression.
            else if (LastContextualParameterIndex == parameter && context.COMMA().Length > 0)
            {
                // Get the last comma in the context.
                var lastComma = context.COMMA().Last();

                // Set the expression range if the last child in the context is a comma.
                if (lastComma == context.children.Last())
                    // Set the range to be the end of the comma to the start of the call range.
                    OrderedParameters[parameter].ExpressionRange = new DocRange(
                        DocRange.GetRange(lastComma).end,
                        functionCallRange.end
                    );
            }
        }

        public void AddDiagnostics(FileDiagnostics diagnostics)
        {
            foreach (OverloadMatchError error in Errors) diagnostics.Error(error.Message, error.Range);
        }
    
        ///<summary>Gets the restricted calls from the unfilled optional parameters.</summary>
        public void CheckOptionalsRestrictedCalls(ParseInfo parseInfo, DocRange callRange)
        {
            // Iterate through each parameter.
            for (int i = 0; i < OrderedParameters.Length; i++)
                // Check if the parameter is prefilled, which means the parameter is optional and not set.
                if (OrderedParameters[i].Prefilled)
                    // Add the restricted call.
                    foreach (RestrictedCallType callType in Option.Parameters[i].RestrictedCalls)
                        parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                            callType,
                            parseInfo.GetLocation(callRange),
                            new CallStrategy($"An unset optional parameter '{Option.Parameters[i].Name}' in the function '{Option.GetLabel(false)}' calls a restricted value of type '{RestrictedCall.StringFromCallType(callType)}'."))
                        );
        }
    }

    public class OverloadMatchError
    {
        public string Message { get; }
        public DocRange Range { get; }
        public bool Vital { get; }

        public OverloadMatchError(string message, DocRange range, bool vital = true)
        {
            Message = message;
            Range = range;
            Vital = vital;
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
            MissingParameter     = $"The {{0}} parameter is missing in the {errorName}.";
        }
    }
}