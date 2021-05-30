using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Lambda;
using SignatureHelp = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelp;
using SignatureInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureInformation;
using ParameterInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ParameterInformation;

namespace Deltin.Deltinteger.Parse.Overload
{
    public class OverloadChooser
    {
        public DocRange CallRange { get; }
        public OverloadMatch Match { get; private set; }
        public object AdditionalData { get; private set; }
        public OverloadParameterResult[] ParameterResults { get; private set; }
        public IParameterCallable Overload => Match?.Option.Value;

        readonly ParseInfo _parseInfo;
        readonly Scope _scope;
        readonly Scope _getter;
        readonly OverloadError _errorMessages;
        readonly DocRange _targetRange;
        readonly DocRange _fullRange;
        readonly IOverload[] _overloads;

        CodeType[] _generics;
        bool _genericsProvided;
        int _providedParameterCount;
        DocRange _extraneousParameterRange;
        OverloadMatch[] _matches;

        public OverloadChooser(IOverload[] overloads, ParseInfo parseInfo, Scope elementScope, Scope getter, DocRange targetRange, DocRange callRange, DocRange fullRange, OverloadError errorMessages)
        {
            _overloads = overloads;
            _parseInfo = parseInfo;
            _scope = elementScope;
            _getter = getter;
            _targetRange = targetRange;
            CallRange = callRange;
            _fullRange = fullRange;
            _errorMessages = errorMessages;
            parseInfo.Script.AddOverloadData(this);
        }

        public void Apply(List<ParameterValue> context, bool genericsProvided, CodeType[] generics)
        {
            _genericsProvided = genericsProvided;
            _generics = generics;
            PickyParameter[] inputParameters = ParametersFromContext(context);

            // Match overloads.
            _matches = new OverloadMatch[_overloads.Length];
            for (int i = 0; i < _matches.Length; i++) _matches[i] = MatchOverload(_overloads[i], inputParameters, context);

            // Do nothing else if the number of matches is 0.
            if (_matches.Length == 0) return;

            // Choose the best option.
            Match = BestOption();

            // Get additional data.
            AdditionalData = Overload.Call(_parseInfo, _targetRange);

            // Set parameter results;.
            ParameterResults = new OverloadParameterResult[Overload.Parameters.Length];
            for (int i = 0; i < ParameterResults.Length; i++)
            {
                var pickyParameterInfo = Match.OrderedParameters[i];

                ParameterResults[i] = new OverloadParameterResult(
                    value: pickyParameterInfo.Value,
                    additionalData: Overload.Parameters[i].Validate(_parseInfo, pickyParameterInfo.Value, pickyParameterInfo.ExpressionRange, AdditionalData),
                    refResolvedVariable: pickyParameterInfo.RefVariable,
                    parameterRange: pickyParameterInfo.ExpressionRange);
            }
        }

        private PickyParameter[] ParametersFromContext(List<ParameterValue> context)
        {
            // Return empty if context is null.
            if (context == null) return new PickyParameter[0];

            _providedParameterCount = context.Count;

            // Create the parameters array with the same length as the number of input parameters.
            PickyParameter[] parameters = new PickyParameter[context.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                // If this is the last parameter and there is a proceeding comma, set the extraneous comma range.
                if (i == parameters.Length - 1 && context[i].NextComma != null)
                    _extraneousParameterRange = context[i].NextComma.Range;

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

            // If the generics were provided ('_genericsProvided'), get the type linker from the option.
            OverloadMatch match = new OverloadMatch(option, _genericsProvided ? option.GetTypeLinker(_generics) : null, _parseInfo.TranslateInfo, _targetRange);
            match.OrderedParameters = new PickyParameter[option.Parameters.Length];

            // Check type arg count.
            if (_genericsProvided && _generics.Length != option.TypeArgCount)
                match.IncorrectTypeArgCount(_parseInfo.TranslateInfo, _targetRange);
            
            // Iterate through the option's parameters.
            for (int i = 0; i < inputParameters.Length; i++)
            {
                // Out of range
                if (i >= option.Parameters.Length)
                {
                    continue;
                }
                // Non-picky
                else if (inputParameters[i].ParameterOrdered(option.Parameters[i]))
                {
                    // If the picky parameters end but there is an additional picky parameter, throw a syntax error.
                    if (lastPicky != null && inputParameters[i].Name == null)
                        match.Error($"Named argument '{lastPicky.Name}' is used out-of-position but is followed by an unnamed argument", lastPicky.NameRange);
                    else
                    {
                        match.OrderedParameters[i] = inputParameters[i];

                        // If _genericsFilled is false, get context-inferred type arguments.
                        if (!_genericsProvided)
                            ExtractInferredGenerics(match, option.Parameters[i].GetCodeType(_parseInfo.TranslateInfo), inputParameters[i].Value.Type());

                        // Next contextual parameter
                        if (i == inputParameters.Length - 1 && i < option.Parameters.Length - 1)
                            match.LastContextualParameterIndex = i + 1;
                    }
                }
                else // Picky
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
                                ExtractInferredGenerics(match, option.Parameters[p].GetCodeType(_parseInfo.TranslateInfo), inputParameters[i].Value.Type());
                        }

                    // If the named argument's name is not found, throw an error.
                    if (!nameFound)
                        match.Error($"Named argument '{lastPicky.Name}' does not exist in the function '{option.GetLabel(_parseInfo.TranslateInfo, LabelInfo.OverloadError)}'.", inputParameters[i].NameRange);
                }
            }

            // Compare parameter types.
            match.UpdateAllParameters();

            // Make sure every type arg was accounted for.
            if (!_genericsProvided && option.TypeArgCount != match.TypeArgLinker.Links.Count)
                match.InferSuccessful = false;

            // Get the missing parameters.
            match.GetMissingParameters(_errorMessages, context, _targetRange, CallRange);

            return match;
        }

        private bool ExtractInferredGenerics(OverloadMatch match, CodeType parameterType, CodeType expressionType)
        {
            if (expressionType is UnknownLambdaType)
            {
                // A second pass will be required.
                match.InferSuccessful = false;
                return false;
            }

            bool result = true;

            // If the parameter type is an AnonymousType, add the link for the expression type if it doesn't already exist.
            if (parameterType is AnonymousType pat && pat.Context == match.Option.Trackee)
            {
                var typeLinker = match.TypeArgLinker;

                // If the AnonymousType does not exist in the links, add a new link.
                if (!typeLinker.Links.ContainsKey(pat))
                {
                    typeLinker.Links.Add(pat, expressionType);
                    // Add to the generics array if the generics were not provided.
                    if (!_genericsProvided)
                        match.TypeArgs[match.Option.TypeArgIndexFromAnonymousType(pat)] = expressionType;
                }
                // Otherwise, the link exists. If the expression type is not equal to the link type, add an error.
                else if (!expressionType.Is(typeLinker.Links[pat]))
                    match.InferSuccessful = result = false;
            }
            
            // Recursively match generics.
            for (int i = 0; i < parameterType.Generics.Length; i++)
                // Make sure the expression's type's structure is usable.
                if (i < expressionType.Generics.Length)
                {
                    // Recursively check the generics.
                    if (!ExtractInferredGenerics(match, parameterType.Generics[i], expressionType.Generics[i]))
                        result = false;
                }
                else
                    match.InferSuccessful = result = false;
            
            return result;
        }

        private OverloadMatch BestOption()
        {
            // If there are any methods with no errors, set that as the best option.
            OverloadMatch bestOption = _matches.FirstOrDefault(match => !match.HasError) ?? _matches.FirstOrDefault();

            // Apply the lambdas and method group parameters.
            // Iterate through each parameter.
            for (int i = 0; i < bestOption.OrderedParameters.Length; i++)
            {
                // If the CodeParameter type is a lambda type, get the lambda statement with it.
                if (bestOption.Option.Parameters[i].GetCodeType(_parseInfo.TranslateInfo) is PortableLambdaType portableLambda)
                    bestOption.OrderedParameters[i].LambdaInfo?.FinishAppliers((PortableLambdaType)portableLambda.GetRealType(bestOption.TypeArgLinker));
                // Otherwise, get the lambda statement with the default.
                else
                    bestOption.OrderedParameters[i].LambdaInfo?.FinishAppliers();
            }

            // Type-arg inference will need a second pass after we apply the lambdas,
            // since lambda types are not known until we apply them.
            if (!bestOption.InferSuccessful)
            {
                bool secondPass = true;

                // Extract inferred generics (again)
                for (int i = 0; i < bestOption.OrderedParameters.Length; i++)
                {
                    var parameterValue = bestOption.OrderedParameters[i].Value;

                    if (parameterValue != null)
                        secondPass = secondPass && ExtractInferredGenerics(
                            bestOption,
                            bestOption.Option.Parameters[i].GetCodeType(_parseInfo.TranslateInfo),
                            parameterValue.Type());
                }
                
                // Update the inference status. Will be true if ExtractInferredGenerics returned true with every iteration.
                bestOption.InferSuccessful = secondPass;

                bestOption.UpdateAllParameters();

                // Clear the match's TypeArgLinker.
                if (!secondPass)
                    bestOption.TypeArgLinker = InstanceAnonymousTypeLinker.Empty;
            }  

            // Add the diagnostics of the best option.
            bestOption.AddDiagnostics(_parseInfo.Script.Diagnostics);
            CheckAccessLevel();

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
                _parseInfo.Script.Diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", Overload.GetLabel(_parseInfo.TranslateInfo, LabelInfo.OverloadError)), _targetRange);
        }

        public SignatureHelp GetSignatureHelp(DocPos caretPos)
        {
            // Get the active parameter.
            int activeParameter = -1;

            // Comma with no proceeding value.
            if (_extraneousParameterRange != null && (_extraneousParameterRange.Start + CallRange.End).IsInside(caretPos))
                activeParameter = _providedParameterCount;
            // Parameter
            else if (ParameterResults != null)
                // Loop through parameter ranges while activeParameter is -1.
                for (int i = 0; i < ParameterResults.Length && activeParameter == -1; i++)
                    // If the proved caret position is inside the parameter range, set it as the active parameter.
                    if (ParameterResults[i].ParameterRange != null && ParameterResults[i].ParameterRange.IsInside(caretPos))
                        activeParameter = i;

            // Get the signature information.
            SignatureInformation[] signatureInformations = new SignatureInformation[_matches.Length];
            int activeSignature = -1;
            for (int i = 0; i < signatureInformations.Length; i++)
            {
                var match = _matches[i];
                var overload = match.Option;

                // If the chosen overload matches the overload being iterated upon, set the active signature.
                if (Overload == overload.Value)
                    activeSignature = i;

                // Get the parameter information for the signature.
                var parameters = new ParameterInformation[overload.Parameters.Length];

                // Convert parameters to parameter information.
                for (int p = 0; p < parameters.Length; p++)
                    parameters[p] = new ParameterInformation()
                    {
                        // Get the label to show in the signature.
                        Label = overload.Parameters[p].GetLabel(_parseInfo.TranslateInfo, new AnonymousLabelInfo(match.TypeArgLinker)),
                        // Get the documentation.
                        Documentation = overload.Parameters[p].Documentation
                    };

                // Create the signature information.
                signatureInformations[i] = new SignatureInformation()
                {
                    Label = overload.GetLabel(_parseInfo.TranslateInfo, new LabelInfo() {
                        IncludeDocumentation = false,
                        IncludeParameterNames = true,
                        IncludeParameterTypes = true,
                        IncludeReturnType = true,
                        AnonymousLabelInfo = new AnonymousLabelInfo(match.TypeArgLinker)
                    }),
                    Parameters = parameters,
                    Documentation = overload.Documentation
                };
            }

            return new SignatureHelp()
            {
                ActiveParameter = activeParameter,
                ActiveSignature = activeSignature,
                Signatures = signatureInformations
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
        /// <summary>The resolved variable for 'ref' parameters.</summary>
        public VariableResolve RefVariable { get; set; }

        public PickyParameter(bool prefilled)
        {
            Prefilled = prefilled;
        }

        public bool ParameterOrdered(CodeParameter parameter) => !Picky || parameter.Name == Name;
    }

    public class OverloadMatch : IVariableResolveErrorHandler
    {
        public IOverload Option { get; } // The option this OverloadMatch represents.
        public PickyParameter[] OrderedParameters { get; set; } // Parameter data.
        public int LastContextualParameterIndex { get; set; } = -1;
        public InstanceAnonymousTypeLinker TypeArgLinker { get; set; }
        public CodeType[] TypeArgs { get; }
        public bool InferSuccessful { get; set; } = true;
        readonly DeltinScript _deltinScript;
        readonly DocRange _inferenceErrorRange;
        readonly List<OverloadMatchError> _errors = new List<OverloadMatchError>(); // Errors provided by the chooser.
        readonly OverloadMatchError[] _parameterErrors;

        public bool HasError => _errors.Count > 0 || HasParameterErrors; // Does the match have any errors?
        public bool TypeArgLinkerCompleted => TypeArgLinker.Links.Count == Option.TypeArgCount; // Determines if the type-arg linker is completed.
        public bool HasParameterErrors => _parameterErrors.Any(p => p != null); // Are there any parameter errors?

        public OverloadMatch(IOverload option, InstanceAnonymousTypeLinker typeArgLinker, DeltinScript deltinScript, DocRange inferenceErrorRange)
        {
            Option = option;
            _deltinScript = deltinScript;
            _inferenceErrorRange = inferenceErrorRange;
            _parameterErrors = new OverloadMatchError[option.Parameters.Length];

            if (typeArgLinker != null)
            {
                TypeArgLinker = typeArgLinker;
                TypeArgs = typeArgLinker.TypeArgsFromAnonymousTypes(option.GenericTypes);
            }
            else
            {
                TypeArgLinker = new InstanceAnonymousTypeLinker();
                TypeArgs = new CodeType[option.TypeArgCount];
            }
        }

        public void Error(string message, DocRange range) => _errors.Add(new OverloadMatchError(message, range));

        /// <summary>Confirms that a parameter type matches.</summary>
        public void UpdateParameter(int parameter)
        {
            // Reset the parameter.
            _parameterErrors[parameter] = null;

            // Get the parameter type.
            CodeType parameterType = Option.Parameters[parameter].GetCodeType(_deltinScript).GetRealType(TypeArgLinker);

            // Get the value. Do nothing if there is no value.
            IExpression value = OrderedParameters[parameter]?.Value;
            if (value == null) return;

            DocRange errorRange = OrderedParameters[parameter].ExpressionRange;

            // Lambda arg count mismatch.
            if (parameterType is PortableLambdaType portableParameterType && // Parameter type is a lambda.
                value.Type() is UnknownLambdaType unknownLambdaType && // Value type is a lambda.
                unknownLambdaType.ArgumentCount != portableParameterType.Parameters.Length) // The value lambda's parameter length does not match.
                // Add the error.
                _parameterErrors[parameter] = new($"Lambda does not take {unknownLambdaType.ArgumentCount} arguments", errorRange);
            
            // Do not add other errors if the value's type is an UnknownLambdaType.
            else if (value.Type() is UnknownLambdaType == false)
            {
                // The parameter type does not match.
                if (parameterType.CodeTypeParameterInvalid(value.Type()))
                    _parameterErrors[parameter] = new(string.Format("Cannot convert type '{0}' to '{1}'", value.Type().GetName(), parameterType.GetName()), errorRange);
                
                // Constant used in bad place.
                else if (value.Type() != null && parameterType == null && value.Type().IsConstant())
                    _parameterErrors[parameter] = new($"The type '{value.Type().Name}' cannot be used here", errorRange);
                
                // Invalid ref
                else if (Option.Parameters[parameter].Attributes.Ref)
                {
                    // Resolve the variable.
                    VariableResolve resolve = new VariableResolve(
                        // 'ref' variable settings.
                        options: new VariableResolveOptions() { CanBeIndexed = true, FullVariable = false, ShouldBeSettable = true },
                        expression: value,
                        expressionRange: errorRange,
                        errorHandler: this);

                    OrderedParameters[parameter].RefVariable = resolve;
                }
            }
        }

        public void UpdateAllParameters()
        {
            for (int i = 0; i < OrderedParameters.Length; i++)
                UpdateParameter(i);
        }

        /// <summary>Determines if there are any missing parameters.</summary>
        public void GetMissingParameters(OverloadError messageHandler, List<ParameterValue> context, DocRange targetRange, DocRange signatureRange)
        {
            for (int i = 0; i < OrderedParameters.Length; i++)
                if (OrderedParameters[i]?.Value == null)
                {
                    if (OrderedParameters[i] == null) OrderedParameters[i] = new PickyParameter(true);
                    AddContextualParameter(context, signatureRange, targetRange, i);

                    // Default value
                    if (Option.Parameters[i].DefaultValue != null)
                        // Set the default value.
                        OrderedParameters[i].Value = Option.Parameters[i].DefaultValue;
                    else
                        // Parameter is missing.
                        Error(string.Format(messageHandler.MissingParameter, Option.Parameters[i].Name), targetRange);
                }
        }

        private void AddContextualParameter(List<ParameterValue> context, DocRange targetRange, DocRange signatureRange, int parameter)
        {
            // No parameters set, set range for first parameter to callRange.
            if (parameter == 0 && OrderedParameters.All(p => p?.Value == null))
                OrderedParameters[0].ExpressionRange = targetRange;
            // If this is the last contextual parameter and the context contains comma, set the expression range so signature help works with the last comma when there is no set expression.
            else if (LastContextualParameterIndex == parameter && parameter < context.Count && context[parameter].NextComma != null)
                // Set the range to be the end of the comma to the start of the call range.
                OrderedParameters[parameter].ExpressionRange = context[parameter].NextComma.Range.End + signatureRange.End;
        }

        public void AddDiagnostics(FileDiagnostics diagnostics)
        {
            // Inference error
            if (!InferSuccessful)
                diagnostics.Error($"The type arguments for method '{Option.GetLabel(_deltinScript, LabelInfo.OverloadError)}' cannot be inferred from the usage. Try specifying the type arguments explicitly.", _inferenceErrorRange);

            // Parameter errors
            foreach (var parameterError in _parameterErrors)
                if (parameterError != null)
                    diagnostics.Error(parameterError.Message, parameterError.Range);

            // Other errors
            foreach (OverloadMatchError error in _errors) diagnostics.Error(error.Message, error.Range);
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
                            RestrictedCall.Message_UnsetOptionalParameter(Option.Parameters[i].Name, Option.GetLabel(parseInfo.TranslateInfo, LabelInfo.OverloadError), callType),
                            Option.RestrictedValuesAreFatal
                        ));
        }
    
        public void IncorrectTypeArgCount(DeltinScript deltinScript, DocRange range) =>
            Error("The function '" + Option.GetLabel(deltinScript, LabelInfo.OverloadError) + "' requires " + Option.TypeArgCount + " type arguments", range);
                
        class OverloadMatchError
        {
            public string Message;
            public DocRange Range;

            public OverloadMatchError(string message, DocRange range)
            {
                Message = message;
                Range = range;
            }
        }
    }

    public class OverloadError
    {
        public string BadParameterCount { get; set; }
        public string ParameterDoesntExist { get; set; }
        public string MissingParameter { get; set; }

        public OverloadError(string errorName)
        {
            BadParameterCount = $"No overloads for the {errorName} has {{0}} parameters.";
            ParameterDoesntExist = $"The parameter '{{0}}' does not exist in the {errorName}.";
            MissingParameter = $"The {{0}} parameter is missing in the {errorName}.";
        }
    }
}