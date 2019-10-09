using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Antlr4.Runtime;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger
{
    public class SyntaxErrorException : Exception
    {
        public Location Location { get; }

        public bool TrackLocation { get; set; } = false;

        public SyntaxErrorException(string message, Location location) : base(message)
        {
            Location = location;
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
        public const string incorrectEnumType    = "Expected enum of type '{0}', got '{1}' instead.";
        public const string variableIsReadonly   = "Variable '{0}' is readonly.";
        public const string cantReadVariable     = "Can't read the variable '{0}'.";
        public const string expectedVariable     = "Expected a variable.";
        public const string typeDoesNotExist     = "The type '{0}' does not exist.";
        public const string thisCantBeUsed       = "The 'this' keyword cannot be used here.";
        public const string importFileNotFound   = "The file '{0}' could not be found.";
        public const string invalidPathChars     = "The path '{0}' contains invalid characters.";
        public const string alreadyImported      = "The file '{0}' was already imported.";
        public const string selfImport           = "Can't import own file.";
        public const string typeNameConflict     = "The type name '{0}' conflicts with an predefined workshop type.";
        public const string nameAlreadyDefined   = "A type of the same name was already defined.";
        public const string invalidValueType     = "Expected value of type '{0}'.";
        public const string invalidVarRefType    = "'{0}' must be {1} variable.";
        #endregion

        public static SyntaxErrorException StringParameterCount(int parameterIndex, int parameterCount, Location location)
        {
            return new SyntaxErrorException(
                string.Format(SyntaxErrorException.parameterCount, parameterIndex, parameterCount),
                location
            );
        }

        public static SyntaxErrorException StringParseFailed(string str, Location location)
        {
            return new SyntaxErrorException(
                string.Format(stringParse, str),
                location
            );
        }

        public static SyntaxErrorException InvalidString(string str, Location location)
        {
            return new SyntaxErrorException(
                string.Format(invalidString, str),
                location
            );
        }

        public static SyntaxErrorException NonexistentMethod(string name, Location location)
        {
            return new SyntaxErrorException(
                string.Format(methodDoesntExist, name),
                location
            );
        }

        public static SyntaxErrorException MissingParameter(string parameterName, string methodName, Location location)
        {
            return new SyntaxErrorException(
                string.Format(missingParameter, parameterName, methodName),
                location
            );
        }
    
        public static SyntaxErrorException VariableDoesNotExist(string variableName, Location location)
        {
            return new SyntaxErrorException(
                string.Format(variableDoesNotExist, variableName),
                location
            );
        }

        public static SyntaxErrorException AlreadyDefined(string variableName, Location location)
        {
            return new SyntaxErrorException(
                string.Format(alreadyDefined, variableName),
                location
            );
        }

        public static SyntaxErrorException InvalidMethodType(bool needsToBeValue, string methodName, Location location)
        {
            string err = needsToBeValue? mustBeValue : mustBeAction;
            return new SyntaxErrorException(
                string.Format(err, methodName),
                location
            );
        }

        public static SyntaxErrorException RecursionNotAllowed(Location location)
        {
            return new SyntaxErrorException(
                recursionNotAllowed,
                location
            );
        }

        public static SyntaxErrorException EnumCantBeValue(string @enum, Location location)
        {
            return new SyntaxErrorException(
                string.Format(enumCantBeValue, @enum),
                location
            );
        }

        public static SyntaxErrorException TooManyParameters(string methodName, int parameterCount, int gotCount, Location location)
        {
            return new SyntaxErrorException(
                string.Format(tooManyParameters, methodName, parameterCount, gotCount),
                location
            );
        }
    
        public static SyntaxErrorException NotAConstructor(TypeKind typeKind, string typeName, int parameterCount, Location location)
        {
            return new SyntaxErrorException(
                string.Format(notAConstructor, typeKind.ToString().ToLower(), typeName, parameterCount),
                location
            );
        }
    
        public static SyntaxErrorException ConstructorName(Location location)
        {
            return new SyntaxErrorException(constructorName, location);
        }
    
        public static SyntaxErrorException ExpectedEnumGotValue(string enumName, Location location)
        {
            return new SyntaxErrorException(string.Format(expectedEnumGotValue, enumName), location);
        }
    
        public static SyntaxErrorException VariableIsReadonly(string variableName, Location location)
        {
            return new SyntaxErrorException(string.Format(variableIsReadonly, variableName), location);
        }
    
        public static SyntaxErrorException CantReadVariable(string variableName, Location location)
        {
            return new SyntaxErrorException(string.Format(cantReadVariable, variableName), location);
        }

        public static SyntaxErrorException ExpectedVariable(Location location)
        {
            return new SyntaxErrorException(expectedVariable, location);
        }
    
        public static SyntaxErrorException NonexistentType(string typeName, Location location)
        {
            return new SyntaxErrorException(string.Format(typeDoesNotExist, typeName), location);
        }
    
        public static SyntaxErrorException ThisCantBeUsed(Location location)
        {
            throw new SyntaxErrorException(thisCantBeUsed, location);
        }
    
        public static SyntaxErrorException ImportFileNotFound(string fullPath, Location location)
        {
            return new SyntaxErrorException(string.Format(importFileNotFound, fullPath), location);
        }
    
        public static SyntaxErrorException InvalidImportPathChars(string path, Location location)
        {
            return new SyntaxErrorException(string.Format(invalidPathChars, path), location);
        }

        public static SyntaxErrorException SelfImport(Location location)
        {
            return new SyntaxErrorException(selfImport, location);
        }
    
        public static SyntaxErrorException TypeNameConflict(string name, Location location)
        {
            return new SyntaxErrorException(string.Format(typeNameConflict, name), location);
        }

        public static SyntaxErrorException NameAlreadyDefined(Location location)
        {
            return new SyntaxErrorException(nameAlreadyDefined, location);
        }
    
        public static SyntaxErrorException InvalidValueType(string expected, string got, Location location)
        {
            return new SyntaxErrorException(string.Format(invalidValueType, expected), location);
        }

        public static SyntaxErrorException IncorrectEnumType(string expected, string got, Location location)
        {
            return new SyntaxErrorException(string.Format(incorrectEnumType, expected, got), location);
        }
    
        public static SyntaxErrorException InvalidVarRefType(string name, VarType varType, Location location)
        {
            if (varType == VarType.Indexed)
                return new SyntaxErrorException(string.Format(invalidVarRefType, name, "an indexed"), location);
            else if (varType == VarType.Model)
                return new SyntaxErrorException(string.Format(invalidVarRefType, name, "a model"), location);
            else if (varType == VarType.Image)
                return new SyntaxErrorException(string.Format(invalidVarRefType, name, "an image"), location);
            else if (varType == VarType.PathMap)
                return new SyntaxErrorException(string.Format(invalidVarRefType, name, "a path map"), location);
            else
                throw new NotImplementedException();
        }
    }

    public enum VarType
    {
        Indexed,
        Model,
        Image,
        PathMap
    }
}
