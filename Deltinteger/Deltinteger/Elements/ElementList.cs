using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Elements
{
    public class ElementList : IMethod
    {
        public string Name { get; }
        public CodeParameter[] Parameters { get; private set; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public string Documentation { get; }
        public CodeType ReturnType { get; private set; }
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
                    ExpressionOrWorkshopValue defaultValue = null;
                    if (function.Parameters[i].HasDefaultValue)
                        defaultValue = new ExpressionOrWorkshopValue(function.Parameters[i].GetDefaultValue());
                    
                    // TODO: Restricted value.
                    // If the default parameter value is an Element and the Element is restricted,
                    // if (defaultValue is Element parameterElement && parameterElement.Function.Restricted != null)
                        // ...then add the restricted call type to the parameter's list of restricted call types.
                        // Parameters[i].RestrictedCalls.Add((RestrictedCallType)parameterElement.Function.Restricted);
                    
                    // Set the parameter.
                    Parameters[i] = new CodeParameter(name, documentation, type, defaultValue);
                }
            }
        }

        ElementList(ElementJsonValue value, ITypeSupplier typeSupplier) : this((ElementBaseJson)value, typeSupplier)
        {
            ReturnType = typeSupplier.FromString(value.ReturnType);
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

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(!DoesReturnValue ? null : ReturnType?.Name ?? "define", Name, Parameters, markdown, Documentation);

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (_restricted != null)
                // If there is a restricted call type, add it.
                parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                    (RestrictedCallType)_restricted,
                    parseInfo.GetLocation(callRange),
                    RestrictedCall.Message_Element((RestrictedCallType)_restricted)
                ));
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