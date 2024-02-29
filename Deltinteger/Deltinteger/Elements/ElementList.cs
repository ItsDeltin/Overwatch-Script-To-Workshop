using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Elements
{
    public class ElementList : IMethod, IGetRestrictedCallTypes
    {
        public string Name { get; }
        public CodeParameter[] Parameters { get; private set; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public MarkupBuilder Documentation { get; }
        public ICodeTypeSolver CodeType { get; private set; }
        IMethodExtensions IMethod.MethodInfo { get; } = new MethodInfo();
        private readonly RestrictedCallType? _restricted;
        private readonly ElementBaseJson _function;
        private readonly Element _actionReturnValue;

        // IScopeable defaults
        public LanguageServer.Location DefinedAt { get; } = null;
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public bool WholeContext { get; } = true;
        public bool Static => true;
        public bool DoesReturnValue => _function is ElementJsonValue || _actionReturnValue != null;

        ElementList(ElementBaseJson function)
        {
            _function = function;

            Name = function.CodeName();
            Documentation = function.Documentation;

            Attributes.GetRestrictedCallTypes = this;

            if (function.Restricted != null)
                _restricted = RestrictedCall.GetRestrictedCallTypeFromString(function.Restricted);

            // Get the parameters.
            if (function.Parameters == null) Parameters = new CodeParameter[0];
            else Parameters = new CodeParameter[function.Parameters.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                // Get the name and documentation.
                string name = function.Parameters[i].Name.Replace(" ", "");
                string documentation = function.Parameters[i].Documentation;

                // If 'VariableReferenceIsGlobal' is not null, the parameter is a variable reference.
                if (function.Parameters[i].VariableReferenceIsGlobal != null)
                {
                    // Set the parameter as a variable reference parameter.
                    Parameters[i] = new VariableParameter(
                        name,
                        documentation,
                        function.Parameters[i].VariableReferenceIsGlobal.Value ? VariableType.Global : VariableType.Player,
                        new CodeTypeFromStringSolver("Any"),
                        new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true }
                    );
                }
                else // Not a variable reference parameter.
                {
                    // The type of the parameter.
                    string type = function.Parameters[i].Type ?? "Any";

                    // Get the default value.
                    IWorkshopTree defaultValueWorkshop = null;
                    ExpressionOrWorkshopValue defaultValue = null;
                    if (function.Parameters[i].HasDefaultValue)
                    {
                        defaultValueWorkshop = function.Parameters[i].GetDefaultValue();
                        defaultValue = new ExpressionOrWorkshopValue(defaultValueWorkshop);
                    }

                    // Set the parameter.
                    Parameters[i] = new CodeParameter(name, documentation, new CodeTypeFromStringSolver(type), defaultValue);

                    // If the default parameter value is an Element and the Element is restricted,
                    if (defaultValueWorkshop is Element parameterElement && parameterElement.Function.Restricted != null)
                    {
                        var restrictedParameterType = RestrictedCall.GetRestrictedCallTypeFromString(parameterElement.Function.Restricted);

                        // ...then add the restricted call type to the parameter's list of restricted call types.
                        if (restrictedParameterType is not null)
                            Parameters[i].RestrictedCalls.Add(restrictedParameterType.Value);
                    }
                }
            }
        }

        ElementList(ElementJsonValue value) : this((ElementBaseJson)value)
        {
            CodeType = new CodeTypeFromStringSolver(value.ReturnType);
        }

        ElementList(ElementJsonAction action) : this((ElementBaseJson)action)
        {
            if (action.ReturnValue != null)
            {
                var returnValue = (ElementJsonValue)ElementRoot.Instance.GetFunction(action.ReturnValue);
                _actionReturnValue = Element.Part(returnValue);
                CodeType = new CodeTypeFromStringSolver(returnValue.ReturnType);
            }
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            Element element = Element.Part(_function, methodCall.ParameterValues);
            element.Comment = methodCall.ActionComment;

            if (element.Function is ElementJsonAction)
            {
                actionSet.AddAction(element);

                if (_actionReturnValue != null)
                    return _actionReturnValue;
                return null;
            }
            else return element;
        }

        public object Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (_restricted != null)
                // If there is a restricted call type, add it.
                parseInfo.RestrictedCallHandler.AddRestrictedCall(new RestrictedCall(
                    (RestrictedCallType)_restricted,
                    parseInfo.GetLocation(callRange),
                    RestrictedCall.Message_Element((RestrictedCallType)_restricted)
                ));

            return null;
        }

        public IEnumerable<RestrictedCallType> GetRestrictedCallTypes() =>
            _restricted == null ?
            Enumerable.Empty<RestrictedCallType>() :
            new RestrictedCallType[] { (RestrictedCallType)_restricted };

        private static IMethod[] WorkshopFunctions { get; } = GetWorkshopFunctions();
        private static IMethod[] GetWorkshopFunctions()
        {
            // Initialize the list.
            List<IMethod> functions = new List<IMethod>();

            // Get the actions.
            foreach (var action in ElementRoot.Instance.Actions)
                if (!action.IsHidden)
                    functions.Add(new ElementList(action));

            // Get the values.
            foreach (var value in ElementRoot.Instance.Values)
                if (!value.IsHidden)
                    functions.Add(new ElementList(value));

            return functions.ToArray();
        }

        public static void AddWorkshopFunctionsToScope(Scope scope, ITypeSupplier typeSupplier)
        {
            foreach (var function in WorkshopFunctions)
                scope.AddNativeMethod(function);
        }
    }
}