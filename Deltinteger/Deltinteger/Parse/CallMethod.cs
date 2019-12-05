using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CallMethodAction : CodeAction, IExpression, IStatement
    {
        public IMethod CallingMethod { get; }
        private DeltinScript translateInfo { get; }

        public CallMethodAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.MethodContext methodContext)
        {
            this.translateInfo = translateInfo;
            string methodName = methodContext.PART().GetText();

            if (methodContext.picky_parameters() == null)
            {
                // Get the parameter values
                IExpression[] parameterValues;
                DeltinScriptParser.ExprContext[] parameterContexts;

                // Set parameterValues and parameterContexts.
                if (methodContext.call_parameters() == null)
                {
                    // If call_parameters is null, set both as empty.
                    parameterValues = new IExpression[0];
                    parameterContexts = new DeltinScriptParser.ExprContext[0];
                }
                else
                {
                    // Get the parameter values.
                    parameterContexts = methodContext.call_parameters().expr();
                    parameterValues = new IExpression[parameterContexts.Length];
                    for (int i = 0; i < parameterValues.Length; i++)
                        parameterValues[i] = GetExpression(script, translateInfo, scope, parameterContexts[i]);
                }

                // Get the best overload via types.
                var methods = scope.GetMethodsByName(methodName)
                    // Order the list by the number of parameters in each method.
                    .OrderBy(m => m.Parameters.Length)
                    .ToList();
                
                CallingMethod = methods.OrderBy(m => Math.Abs(parameterValues.Length - m.Parameters.Length)).First();

                // Syntax error if there are no methods with the name.
                if (methods.Count == 0)
                    script.Diagnostics.Error(string.Format("No method by the name of {0} exists in the current context.", methodName), DocRange.GetRange(methodContext.PART()));
                else
                {
                    methods = methods.Where(m => m.Parameters.Length >= parameterValues.Length)
                        .ToList();
                    
                    if (methods.Count == 0)
                        script.Diagnostics.Error(
                            string.Format("No overloads for the method {0} has {1} parameters.", methodName, parameterValues.Length),
                            DocRange.GetRange(methodContext.PART())
                        );
                    else
                    {
                        var methodDiagnostics = new Dictionary<IMethod, List<Diagnostic>>();
                        // Fill methodDiagnostics.
                        foreach (var method in methods) methodDiagnostics.Add(method, new List<Diagnostic>());

                        // Match by value types and parameter types.
                        for (int i = 0; i < parameterValues.Length; i++)
                        {
                            // Get the type of the parameter value.
                            var valueType = parameterValues[i].Type();

                            // Check each method to make sure the parameter matches.
                            foreach (var method in methods)
                            if (!CodeType.TypeMatches(method.Parameters[i].Type, valueType))
                            {
                                // The parameter type does not match.
                                string msg = string.Format("Expected a value of type {0}.", method.Parameters[i].Type.Name);
                                methodDiagnostics[method].Add(new Diagnostic(msg, DocRange.GetRange(parameterContexts[i])));
                            }
                        }

                        // If there are any methods with no errors, set that as the chosen method.
                        var methodWithNoErrors = methodDiagnostics.FirstOrDefault(m => m.Value.Count > 0).Key;
                        if (methodWithNoErrors != null) CallingMethod = methodWithNoErrors;

                        // Add the diagnostics of the chosen method.
                        script.Diagnostics.AddDiagnostics(methodDiagnostics[CallingMethod].ToArray());
                    }
                }
            }
            else
            {
                // todo: this
                throw new NotImplementedException();
            }
        }

        public Scope ReturningScope()
        {
            if (CallingMethod == null) return null;

            if (CallingMethod.ReturnType == null)
                return translateInfo.PlayerVariableScope;
            else
                return CallingMethod.ReturnType.GetObjectScope();
        }

        public CodeType Type() => CallingMethod?.ReturnType;
    }
}