using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchParser
{
    public class IncorrectElementTypeException : ArgumentException
    {
        public IncorrectElementTypeException(string paramName, bool needsToBeValue) : base(needsToBeValue ? $"{paramName} is an action, not a value." : $"{paramName} is a value, not an action.", paramName) {}
    }

    public class InvalidStringException : ArgumentException
    {
        public InvalidStringException(string value) : base($"\"{value}\" is not a valid Overwatch string.") {}
    }

    public class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message, int row, int column) : base($"Syntax error at {row},{column}: {message}") {}
        public SyntaxErrorException(string message) : base($"Syntax error: {message}") {}
    }
}
