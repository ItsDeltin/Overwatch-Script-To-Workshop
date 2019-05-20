using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace OverwatchParser
{
    public class IncorrectElementTypeException : ArgumentException
    {
        public IncorrectElementTypeException(string paramName, bool needsToBeValue) : base(needsToBeValue ? $"{paramName} is an action, not a value." : $"{paramName} is a value, not an action.", paramName) {}
    }

    /*
    public class InvalidStringException : ArgumentException
    {
        public InvalidStringException(string value) : base(value) {}
    }
    */

    public class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message, IToken token) : base($"Syntax error at {token.Line},{token.Column}: {message}") {}
        public SyntaxErrorException(string message) : base($"Syntax error: {message}") {}
    }
}
