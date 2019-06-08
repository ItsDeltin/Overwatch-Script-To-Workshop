using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger
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
        const string msg = "Syntax error at {0},{1}: {2}";

        public readonly Range Range;
        public SyntaxErrorException(string message, Deltin.Deltinteger.Parse.Range range) : base(string.Format(msg, range.start, range.end, message))
        {
            Range = range;
        }

        public SyntaxErrorException(string message) : base($"Syntax error: {message}") {}
    }
}
