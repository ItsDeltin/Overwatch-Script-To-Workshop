using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Methods.Overloads
{
    using Utility;
    using Diagnostics;
    using Expressions;

    class OverloadChooser : IDisposable
    {
        readonly DisposableCollection staticUnmanaged = new DisposableCollection();

        readonly SerializedDisposableCollection stateUnmanaged = new SerializedDisposableCollection();


        /// <summary>The current analysis context.</summary>
        readonly ContextInfo context;

        /// <summary>If the parameter list ends with a comma instead of an expression
        /// then this will be the range of that comma. Otherwise, it will be null.</summary>
        DocRange extraneousParameterRange;

        /// <summary>The parameters defined by the user.</summary>
        PickyParameter[] definedParameters;

        public OverloadChooser(ContextInfo context, List<ParameterValue> parameterSyntax)
        {
            this.context = context;

            ParametersFromSyntax(parameterSyntax);

            // Observe parameter expressions
            staticUnmanaged.Add(Helper.Observe(from p in definedParameters select p.Value, valueData =>
            {
                // Reset
                stateUnmanaged.Dispose();
            }));
        }

        public void Dispose()
        {
            staticUnmanaged.Dispose();
            stateUnmanaged.Dispose();
        }

        private void ParametersFromSyntax(List<ParameterValue> syntax)
        {
            // Empty if context is null.
            if (syntax == null)
            {
                definedParameters = new PickyParameter[0];
                return;
            }

            // Create the parameters array with the same length as the number of input parameters.
            definedParameters = new PickyParameter[syntax.Count];
            for (int i = 0; i < definedParameters.Length; i++)
            {
                // If this is the last parameter and there is a proceeding comma, set the extraneous comma range.
                if (i == definedParameters.Length - 1 && syntax[i].NextComma != null)
                    extraneousParameterRange = syntax[i].NextComma.Range;

                Token name = null;
                if (name = syntax[i].PickyParameter)
                {
                    // Check if there are any duplicate names.
                    if (definedParameters.Any(p => p != null && p.Picky && p.Name == name.Text))
                        // If there are, syntax error
                        staticUnmanaged.Add(context.Error(Messages.PickyParameterAlreadySet(name), name));
                }

                // Set expression and expressionRange.
                // parameter.LambdaInfo = new ExpectingLambdaInfo();
                // parameter.Value = context.SetLambdaInfo(parameter.LambdaInfo).GetExpression(syntax[i].Expression);
                // parameter.ExpressionRange = syntax[i].Expression.Range;
                Expression value = context.GetExpression(syntax[i].Expression);

                definedParameters[i] = new PickyParameter(name, value, syntax[i].Expression.Range);
                staticUnmanaged.Add(definedParameters[i]);
            }
        }
    }
}