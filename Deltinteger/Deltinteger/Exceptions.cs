using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Antlr4.Runtime;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger
{
    public class SyntaxErrorException : Exception
    {
        public Range Range { get; private set; }

        public bool TrackLocation { get; set; } = false;

        public SyntaxErrorException(string message, Range range) : base(message)
        {
            Range = range;
        }

        public string GetInfo() 
        {
            string message = Message;
            if (TrackLocation)
            {
                var frame = new StackTrace(this, true).GetFrame(0);
                message += " (from " + Path.GetFileName(frame.GetFileName()) + " at line " + frame.GetFileLineNumber() + ", column " + frame.GetFileColumnNumber() + ")";
            }
            return message;
        }

        #region Messages
        public const string parameterCount       = "Can't set the <{0}> format, there are only {1} parameters.";
        public const string stringParse          = "Failed to parse the string \"{0}\".";
        public const string invalidString        = "\"{0}\" is not a valid string.";
        public const string methodDoesntExist    = "The method \"{0}\" does not exist.";
        public const string missingParameter     = "Missing parameter \"{0}\" in the method \"{1}\" and no default type to fallback on.";
        public const string tooManyParameters    = "The {0} method takes {1} parameters, got {2} instead.";
        public const string invalidEnumValue     = "The value '{0}' does not exist in the enum '{1}'.";
        public const string variableDoesNotExist = "The variable '{0}' does not exist.";
        public const string alreadyDefined       = "The variable '{0}' was already defined.";
        public const string mustBeValue          = "{0} must be a value method, not an action method.";
        public const string mustBeAction         = "{0} must be an action method, not a value method.";
        public const string recursionNotAllowed  = "Recursion is not allowed here. Do 'recursive method' instead of 'method' to allow recursion.";
        public const string enumCantBeValue      = "The enum '{0}' cannot be used like a value.";
        public const string notAConstructor      = "No constructors in the {0} '{1}' have {2} parameters.";
        public const string constructorName      = "Constructor name must be the same as the type name.";
        public const string expectedEnumGotValue = "Expected the enum '{0}', got a value instead.";
        public const string variableIsReadonly   = "Variable '{0}' is readonly.";
        public const string expectedVariable     = "Expected a variable.";
        public const string typeDoesNotExist     = "The type '{0}' does not exist.";
        public const string thisCantBeUsed       = "The 'this' keyword cannot be used here.";
        public const string importFileNotFound   = "The file '{0}' could not be found.";
        public const string invalidPathChars     = "The path '{0}' contains invalid characters.";
        #endregion

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

        public static SyntaxErrorException RecursionNotAllowed(Range range)
        {
            return new SyntaxErrorException(
                recursionNotAllowed,
                range
            );
        }

        public static SyntaxErrorException EnumCantBeValue(string @enum, Range range)
        {
            return new SyntaxErrorException(
                string.Format(enumCantBeValue, @enum),
                range
            );
        }

        public static SyntaxErrorException TooManyParameters(string methodName, int parameterCount, int gotCount, Range range)
        {
            return new SyntaxErrorException(
                string.Format(tooManyParameters, methodName, parameterCount, gotCount),
                range
            );
        }
    
        public static SyntaxErrorException NotAConstructor(TypeKind typeKind, string typeName, int parameterCount, Range range)
        {
            return new SyntaxErrorException(
                string.Format(notAConstructor, typeKind.ToString().ToLower(), typeName, parameterCount),
                range
            );
        }
    
        public static SyntaxErrorException ConstructorName(Range range)
        {
            return new SyntaxErrorException(constructorName, range);
        }
    
        public static SyntaxErrorException ExpectedEnumGotValue(string enumName, Range range)
        {
            return new SyntaxErrorException(string.Format(expectedEnumGotValue, enumName), range);
        }
    
        public static SyntaxErrorException VariableIsReadonly(string variableName, Range range)
        {
            return new SyntaxErrorException(string.Format(variableIsReadonly, variableName), range);
        }
    
        public static SyntaxErrorException ExpectedVariable(Range range)
        {
            return new SyntaxErrorException(expectedVariable, range);
        }
    
        public static SyntaxErrorException NonexistentType(string typeName, Range range)
        {
            return new SyntaxErrorException(string.Format(typeDoesNotExist, typeName), range);
        }
    
        public static SyntaxErrorException ThisCantBeUsed(Range range)
        {
            throw new SyntaxErrorException(thisCantBeUsed, range);
        }
    
        public static SyntaxErrorException ImportFileNotFound(string fullPath, Range range)
        {
            return new SyntaxErrorException(string.Format(importFileNotFound, fullPath), range);
        }
    
        public static SyntaxErrorException InvalidImportPathChars(string path, Range range)
        {
            return new SyntaxErrorException(string.Format(invalidPathChars, path), range);
        }
    }
}
