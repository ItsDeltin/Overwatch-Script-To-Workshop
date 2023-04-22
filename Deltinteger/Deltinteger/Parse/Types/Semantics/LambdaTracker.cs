using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Parse
{
    /// <summary>
    /// Tracks uses of lambda types in the script.
    /// </summary>
    class LambdaTracker : ITypeArgTrackee
    {
        /// <summary>[0] will be the return type if LambdaReturnsValue is true. Every other value is for parameters.</summary>
        public AnonymousType[] GenericTypes { get; }

        /// <summary>Does this lambda return a value?</summary>
        public bool LambdaReturnsValue { get; }

        /// <summary>Is the lambda constant?</summary>
        public bool IsConstant { get; }

        public int GenericsCount => GenericTypes.Length;

        private LambdaTracker(AnonymousType[] genericTypes, bool returnsValue, bool isConstant)
        {
            GenericTypes = genericTypes;
            LambdaReturnsValue = returnsValue;
            IsConstant = isConstant;
        }

        /// <summary>Creates a TypeArgCall for resolving generics for a lambda.</summary>
        /// <param name="returnType">The return type of the lambda. Null if the lambda does not return a value.</param>
        /// <param name="parameters">The parameter types of the lambda.</param>
        /// <returns>A TypeArgCall that can be added to the script.</returns>
        public static TypeArgCall CreateTrackingCall(CodeType returnType, CodeType[] parameters, bool isConstant)
        {
            // Generate the AnonymousTypes for the lambda.
            // 'AnonymousType' is a CodeType and in this scenario isn't actually used as a type for any reason.
            // This is fairly hacky, so GlobTypeArgCollector should introduce compatibility for a more abstract
            // concept of what an anonymous type is, but for now this works fine.

            // If the lambda returns a value, [0] will be the return type for both 'anonymousTypes' and 'callParams'.
            List<AnonymousType> anonymousTypes = new List<AnonymousType>();
            IEnumerable<CodeType> callParams = parameters;

            // Does the lambda return a value?
            if (returnType != null)
            {
                // Prepend the return type to the callParams list.
                callParams = callParams.Prepend(returnType);
                // Add a fake anonymous type for the return type.
                anonymousTypes.Add(new AnonymousType("return type", new AnonymousTypeAttributes(false)));
            }

            // Add fake anonymous types for the parameters.
            for (int i = 0; i < parameters.Length; i++)
                anonymousTypes.Add(new AnonymousType(i.ToString(), new AnonymousTypeAttributes(false)));

            // Create the TypeArgCall.
            return new TypeArgCall(new LambdaTracker(anonymousTypes.ToArray(), returnType != null, isConstant), callParams.ToArray());
        }
    }
}