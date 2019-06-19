using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger
{
    public class SyntaxErrorException : Exception
    {
        public Range Range { get; private set; }
        private SyntaxErrorException(string message, Deltin.Deltinteger.Parse.Range range) : base(message)
        {
            Range = range;
        }

        const string parameterCount       = "Can't set the <{0}> format, there are only {1} parameters.";
        const string stringParse          = "Failed to parse the string \"{0}\".";
        const string invalidString        = "\"{0}\" is not a valid string.";
        const string invalidType          = "Expected {0}, got {1} instead.";
        const string methodDoesntExist    = "The method \"{0}\" does not exist.";
        const string missingParameter     = "Missing parameter \"{0}\" in the method \"{1}\" and no default type to fallback on.";
        const string expectedType         = "Expected {0} type \"{1}\" on {2}'s parameter \"{3}\".";
        const string invalidEnumValue     = "The value {0} does not exist in the enum {1}.";
        const string variableDoesNotExist = "The variable {0} does not exist.";
        const string alreadyDefined       = "The variable {0} was already defined.";
        const string mustBeValue    = "{0} must be a value method, not an action method.";
        const string mustBeAction   = "{0} must be an action method, not a value method.";

        public static SyntaxErrorException StringParameterCount(int parameterIndex, int parameterCount, Range range)
        {
            return new SyntaxErrorException(
                string.Format(SyntaxErrorException.parameterCount, parameterIndex, parameterCount),
                range
            );
        }

        public static SyntaxErrorException StringParseFailed(string str, Range range)
        {
            return new SyntaxErrorException(
                string.Format(stringParse, str),
                range
            );
        }

        public static SyntaxErrorException InvalidString(string str, Range range)
        {
            return new SyntaxErrorException(
                string.Format(invalidString, str),
                range
            );
        }

        public static SyntaxErrorException InvalidType(ValueType expected, ValueType got, Range range)
        {
            return new SyntaxErrorException(
                string.Format(invalidType, expected.ToString(), got.ToString()),
                range
            );
        }

        public static SyntaxErrorException NonexistentMethod(string name, Range range)
        {
            return new SyntaxErrorException(
                string.Format(methodDoesntExist, name),
                range
            );
        }

        public static SyntaxErrorException MissingParameter(string parameterName, string methodName, Range range)
        {
            return new SyntaxErrorException(
                string.Format(missingParameter, parameterName, methodName),
                range
            );
        }

        public static SyntaxErrorException ExpectedType(bool value, string typeName, string methodName, string parameterName, Range range)
        {
            return new SyntaxErrorException(
                string.Format(expectedType, (value ? "value" : "enum"), typeName, methodName, parameterName),
                range
            );
        }

        public static SyntaxErrorException InvalidEnumValue(string enumName, string value, Range range)
        {
            return new SyntaxErrorException(
                string.Format(invalidEnumValue, value, enumName),
                range
            );
        }
    
        public static SyntaxErrorException VariableDoesNotExist(string variableName, Range range)
        {
            return new SyntaxErrorException(
                string.Format(variableDoesNotExist, variableName),
                range
            );
        }

        public static SyntaxErrorException AlreadyDefined(string variableName, Range range)
        {
            return new SyntaxErrorException(
                string.Format(alreadyDefined, variableName),
                range
            );
        }

        public static SyntaxErrorException InvalidMethodType(bool needsToBeValue, string methodName, Range range)
        {
            string err = needsToBeValue? mustBeValue : mustBeAction;
            return new SyntaxErrorException(
                string.Format(err, methodName),
                range
            );
        }
    }
}
