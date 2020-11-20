using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Lambda;
using SignatureHelp = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelp;
using SignatureInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureInformation;
using ParameterInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ParameterInformation;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;
using MarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupContent;
using MarkupKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupKind;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse.Overload
{
    public class OverloadChooser
    {
        public DocRange CallRange { get; }
        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly Scope _getter;
        private readonly DocRange _genericErrorRange;
        private readonly OverloadError _errorMessages;

        private readonly IOverload[] _overloads;
        private CodeType[] _generics;
        private bool _genericsProvided;

        public OverloadMatch Match { get; private set; }
        public IParameterCallable Overload => Match?.Option.Value;
        public IExpression[] Values { get; private set; }
        public DocRange[] ParameterRanges { get; private set; }
        public object[] AdditionalParameterData { get; private set; }

        public OverloadChooser(IOverload[] overloads, ParseInfo parseInfo, Scope elementScope, Scope getter, DocRange genericErrorRange, DocRange callRange, OverloadError errorMessages)
        {
            _overloads = overloads;
            _parseInfo = parseInfo;
            _scope = elementScope;
            _getter = getter;
            _genericErrorRange = genericErrorRange;
            _errorMessages = errorMessages;
            CallRange = callRange;

            // todo
            // parseInfo.Script.AddOverloadData(this);
        }

        public void Apply(List<ParameterValue> context, bool genericsProvided, CodeType[] generics)
        {
            _genericsProvided = genericsProvided;
            _generics = generics;
            PickyParameter[] inputParameters = ParametersFromContext(context);

            // Match overloads.
            OverloadMatch[] matches = new OverloadMatch[_overloads.Length];
            for (int i = 0; i < matches.Length; i++) matches[i] = MatchOverload(_overloads[i], inputParameters, context);

            // Choose the best option.
            Match = BestOption(matches);
            Values = Match.OrderedParameters.Select(op => op?.Value).ToArray();
            ParameterRanges = Match.OrderedParameters.Select(op => op?.ExpressionRange).ToArray();

            GetAdditionalData();
        }

        private PickyParameter[] ParametersFromContext(List<ParameterValue> context)
        {
            // Return empty if context is null.
            if (context == null) return new PickyParameter[0];

            // Create the parameters array with the same length as the number of input parameters.
            PickyParameter[] parameters = new PickyParameter[context.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                PickyParameter parameter = new PickyParameter(false);
                parameters[i] = parameter;

                if (parameter.Picky = context[i].PickyParameter != null)
                {
                    // Get the picky name.
                    parameter.Name = context[i].PickyParameter.Text;
                    // Get the name range
                    parameter.NameRange = context[i].PickyParameter.Range;

                    // Check if there are any duplicate names.
                    if (parameters.Any(p => p != null && p.Picky && p != parameter && p.Name == parameter.Name))
                        // If there are, syntax error
                        _parseInfo.Script.Diagnostics.Error($"The parameter {parameter.Name} was already set.", parameter.NameRange);
                }

                // Set expression and expressionRange.
                parameter.LambdaInfo = new ExpectingLambdaInfo();
                parameter.Value = _parseInfo.SetLambdaInfo(parameter.LambdaInfo).GetExpression(_getter, context[i].Expression);
                parameter.ExpressionRange = context[i].Expression.Range;
            }

            return parameters;
        }

        private OverloadMatch MatchOverload(IOverload option, PickyParameter[] inputParameters, List<ParameterValue> context)
        {
            PickyParameter lastPicky = null;

            OverloadMatch match = new OverloadMatch(option);
            match.OrderedParameters = new PickyParameter[option.Parameters.Length];

            // Set the type arg linker.
            // If the generics were provided ('_genericsProvided'), get the type linker from the option.
            // Otherwise if '_genericsProvided' is false, create an empty type linker.
            match.TypeArgLinker = _genericsProvided ? option.GetTypeLinker(_generics) : new InstanceAnonymousTypeLinker();

            // Check type arg count.
            if (_genericsProvided && _generics.Length != option.TypeArgCount)
                match.IncorrectTypeArgCount();
            
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

                        // If _genericsFilled is false, get context-inferred type arguments.
                        if (!_genericsProvided)
                            ExtractInferredGenerics(match.TypeArgLinker, option.Parameters[i].Type, inputParameters[i].Value.Type());

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
                            // A matching parameter was found.
                            match.OrderedParameters[p] = inputParameters[i];
                            nameFound = true;

                            // If _genericsFilled is false, get context-inferred type arguments.
                            if (!_genericsProvided)
                                ExtractInferredGenerics(match.TypeArgLinker, option.Parameters[p].Type, inputParameters[i].Value.Type());
                        }
                    
                    // If the named argument's name is not found, throw an error.
                    if (!nameFound)
                        match.Error($"Named argument '{lastPicky.Name}' does not exist in the function '{option.Label}'.", inputParameters[i].NameRange);
                }
            }

            // Compare parameter types.
            for (int i = 0; i < match.OrderedParameters.Length; i++) match.CompareParameterTypes(i);

            // Get the missing parameters.
            match.GetMissingParameters(_genericErrorRange, _errorMessages, context, CallRange);

            return match;
        }

        // TODO: should this can be moved to the 'InstanceAnonymousTypeLinker' class?
        private void ExtractInferredGenerics(InstanceAnonymousTypeLinker typeLinker, CodeType parameterType, CodeType expressionType)
        {
            // If the parameter type is an AnonymousType, add the link for the expression type if it doesn't already exist.
            // TODO: Add an error if the key already exists and the key's value != expressionType.
            //       If the parameter type is something like 'C<T, T>', providing 'C<Vector, Number>' should add an error.
            //       (Should parameter type checks be handled here?)
            if (parameterType is AnonymousType pat && !typeLinker.Links.ContainsKey(pat))
                typeLinker.Links.Add(pat, expressionType);
            
            // Recursively match generics.
            if (parameterType.Generics != null)
                for (int i = 0; i < parameterType.Generics.Length; i++)
                    // TODO: At this point, if this condition is false then the type structure does not match.
                    if (expressionType.Generics != null && i < expressionType.Generics.Length)
                        // Recursively check the generics.
                        ExtractInferredGenerics(typeLinker, parameterType, expressionType);
        }

        private OverloadMatch BestOption(OverloadMatch[] matches)
        {
            // If there are any methods with no errors, set that as the best option.
            OverloadMatch bestOption = matches.FirstOrDefault(match => !match.HasError) ?? matches.FirstOrDefault(match => !match.HasDeterminingError) ?? matches.FirstOrDefault();

            // Add the diagnostics of the best option.
            bestOption.AddDiagnostics(_parseInfo.Script.Diagnostics);
            CheckAccessLevel();

            // Apply the lambdas and method group parameters.
            // Iterate through each parameter.
            for (int i = 0; i < bestOption.OrderedParameters.Length; i++)
            {
                // If the CodeParameter type is a lambda type, get the lambda statement with it.
                if (bestOption.Option.Parameters[i].Type is PortableLambdaType portableLambda)
                    bestOption.OrderedParameters[i].LambdaInfo?.FinishAppliers(portableLambda);
                // Otherwise, get the lambda statement with the default.
                else
                    bestOption.OrderedParameters[i].LambdaInfo?.FinishAppliers();
            }

            return bestOption;
        }

        private void CheckAccessLevel()
        {
            if (Overload == null) return;

            bool accessable = true;

            if (Overload is IMethod asMethod)
            {
                if (!_getter.AccessorMatches(asMethod)) accessable = false;
            }
            else if (!_getter.AccessorMatches(_scope, Overload.AccessLevel)) accessable = false;

            if (!accessable)
                _parseInfo.Script.Diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", Overload.GetLabel(false)), _genericErrorRange);
        }
    
        private void GetAdditionalData()
        {
            AdditionalParameterData = new object[Overload.Parameters.Length];
            for (int i = 0; i < Overload.Parameters.Length; i++)
                AdditionalParameterData[i] = Overload.Parameters[i].Validate(_parseInfo, Values[i], ParameterRanges.ElementAtOrDefault(i));
        }

        public SignatureHelp GetSignatureHelp(DocPos caretPos)
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
            SignatureInformation[] overloads = new SignatureInformation[_overloads.Length];
            for (int i = 0; i < overloads.Length; i++)
            {
                // Get the parameter information for the signature.
                var parameters = new ParameterInformation[_overloads[i].Parameters.Length];

                // Convert parameters to parameter information.
                for (int p = 0; p < parameters.Length; p++)
                    parameters[p] = new ParameterInformation() {
                        // Get the label to show in the signature.
                        Label = _overloads[i].Parameters[p].GetLabel(),
                        // Get the documentation.
                        Documentation = Extras.GetMarkupContent(_overloads[i].Parameters[p].Documentation)
                    };

                // Create the signature information.
                overloads[i] = new SignatureInformation() {
                    Label = _overloads[i].Label,
                    Parameters = parameters,
                    Documentation = _overloads[i].Documentation
                };
            }

            return new SignatureHelp()
            {
                ActiveParameter = activeParameter,
                ActiveSignature = Array.IndexOf(_overloads, Overload),
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
        /// <summary>When the parameter expressions are parsed, the parameter types are unknown. Lambda and method groups will behave
        /// differently depending on the parameter type, so some components will not be parsed until the parameter type is known.
        /// Use this to apply the lambda/method group data when the overload is chosen.</summary>
        public ExpectingLambdaInfo LambdaInfo { get; set; }

        public PickyParameter(bool prefilled)
        {
            Prefilled = prefilled;
        }

        public bool ParameterOrdered(CodeParameter parameter) => !Picky || parameter.Name == Name;
    }

    public class OverloadMatch
    {
        public IOverload Option { get; }
        public PickyParameter[] OrderedParameters { get; set; }
        public List<OverloadMatchError> Errors { get; } = new List<OverloadMatchError>();
        public bool HasDeterminingError => Errors.Any(error => error.Vital);
        public bool HasError => Errors.Count > 0;
        public int LastContextualParameterIndex { get; set; } = -1;
        public InstanceAnonymousTypeLinker TypeArgLinker { get; set; }

        public OverloadMatch(IOverload option)
        {
            Option = option;
        }

        public void Error(string message, DocRange range, bool vital = true) => Errors.Add(new OverloadMatchError(message, range, vital));

        /// <summary>Confirms that a parameter type matches.</summary>
        public void CompareParameterTypes(int parameter)
        {
            CodeType parameterType = Option.Parameters[parameter].Type.GetRealerType(TypeArgLinker);
            IExpression value = OrderedParameters[parameter]?.Value;
            if (value == null) return;
            DocRange errorRange = OrderedParameters[parameter].ExpressionRange;

            if (parameterType is PortableLambdaType == false || (value is PortableLambdaType portableType && portableType.LambdaKind == LambdaKind.Anonymous))
            {
                if (parameterType.CodeTypeParameterInvalid(value.Type()))
                {
                    // The parameter type does not match.
                    string msg = string.Format("Cannot convert type '{0}' to '{1}'", value.Type().GetNameOrVoid(), parameterType.GetNameOrVoid());
                    Error(msg, errorRange);
                }
                else if (value.Type() != null && parameterType == null && value.Type().IsConstant())
                {
                    string msg = string.Format($"The type '{value.Type().Name}' cannot be used here");
                    Error(msg, errorRange);
                }
            }
        }

        /// <summary>Determines if there are any missing parameters.</summary>
        public void GetMissingParameters(DocRange genericErrorRange, OverloadError messageHandler, List<ParameterValue> context, DocRange functionCallRange)
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

        private void AddContextualParameter(List<ParameterValue> context, DocRange functionCallRange, int parameter)
        {
            // No parameters set, set range for first parameter to callRange.
            if (parameter == 0 && OrderedParameters.All(p => p?.Value == null))
                OrderedParameters[0].ExpressionRange = functionCallRange;
            // If this is the last contextual parameter and the context contains comma, set the expression range so signature help works with the last comma when there is no set expression.
            else if (LastContextualParameterIndex == parameter && parameter < context.Count && context[parameter].NextComma != null)
                // Set the range to be the end of the comma to the start of the call range.
                OrderedParameters[parameter].ExpressionRange = context[parameter].NextComma.Range.End + functionCallRange.End;
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
                            RestrictedCall.Message_UnsetOptionalParameter(Option.Parameters[i].Name, Option.Label, callType)
                        ));
        }
    
        public void IncorrectTypeArgCount()
        {

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