using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Elements
{
    public class ElementList : IMethod
    {
        public string Name { get; }
        public CodeParameter[] Parameters { get; private set; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public string Documentation { get; }
        public CodeType CodeType { get; private set; }
        private readonly RestrictedCallType? _restricted;
        private readonly ElementBaseJson _function;

        // IScopeable defaults
        public LanguageServer.Location DefinedAt { get; } = null;
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public bool WholeContext { get; } = true;
        public bool Static => true;
        public bool DoesReturnValue => _function is ElementJsonValue;

        ElementList(ElementBaseJson function, ITypeSupplier typeSupplier)
        {
            _function = function;

            Name = function.CodeName();
            Documentation = function.Documentation;

            if (function.Restricted != null)
                _restricted = GetRestrictedCallTypeFromString(function.Restricted);

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
                        new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true }
                    );
                }
                else // Not a variable reference parameter.
                {
                    // The type of the parameter.
                    CodeType type = typeSupplier.Default();

                    // Get the type from the type value.
                    if (function.Parameters[i].Type != null)    
                        type = typeSupplier.FromString(function.Parameters[i].Type);

                    // Get the default value.
                    IWorkshopTree defaultValueWorkshop = null;
                    ExpressionOrWorkshopValue defaultValue = null;
                    if (function.Parameters[i].HasDefaultValue)
                    {
                        defaultValueWorkshop = function.Parameters[i].GetDefaultValue();
                        defaultValue = new ExpressionOrWorkshopValue(defaultValueWorkshop);
                    }
                    
                    // Set the parameter.
                    Parameters[i] = new CodeParameter(name, documentation, type, defaultValue);

                    // If the default parameter value is an Element and the Element is restricted,
                    if (defaultValueWorkshop is Element parameterElement && parameterElement.Function.Restricted != null)
                        // ...then add the restricted call type to the parameter's list of restricted call types.
                        Parameters[i].RestrictedCalls.Add(GetRestrictedCallTypeFromString(parameterElement.Function.Restricted));
                }
            }
        }

        ElementList(ElementJsonValue value, ITypeSupplier typeSupplier) : this((ElementBaseJson)value, typeSupplier)
        {
            CodeType = typeSupplier.FromString(value.ReturnType);
        }

        private RestrictedCallType GetRestrictedCallTypeFromString(string value)
        {
            switch (value)
            {
                case "Ability": return RestrictedCallType.Ability;
                case "Attacker": return RestrictedCallType.Attacker;
                case "Event Player": return RestrictedCallType.EventPlayer;
                case "Healer": return RestrictedCallType.Healer;
                case "Knockback": return RestrictedCallType.Knockback;
                default: throw new NotImplementedException("No RestrictedCallType for '" + value + "'");
            }
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            Element element = Element.Part(_function, methodCall.ParameterValues);
            element.Comment = methodCall.ActionComment;

            if (!DoesReturnValue)
            {
                actionSet.AddAction(element);

                if (((ElementJsonAction)_function).ReturnValue != null)
                    return Element.Part(((ElementJsonAction)_function).ReturnValue);
                return null;
            }
            else return element;
        }

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(!DoesReturnValue ? null : CodeType?.Name ?? "define", Name, Parameters, markdown, Documentation);

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);

        public object Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (_restricted != null)
                // If there is a restricted call type, add it.
                parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                    (RestrictedCallType)_restricted,
                    parseInfo.GetLocation(callRange),
                    RestrictedCall.Message_Element((RestrictedCallType)_restricted)
                ));
            
            return null;
        }

        public static IMethod[] GetWorkshopFunctions(ITypeSupplier typeSupplier)
        {
            // Initialize the list.
            List<IMethod> functions = new List<IMethod>();

            // Get the actions.
            foreach (var action in ElementRoot.Instance.Actions)
                if (!action.IsHidden)
                    functions.Add(new ElementList(action, typeSupplier));

            // Get the values.
            foreach (var value in ElementRoot.Instance.Values)
                if (!value.IsHidden)
                    functions.Add(new ElementList(value, typeSupplier));
            
            return functions.ToArray();
        }
    }
}