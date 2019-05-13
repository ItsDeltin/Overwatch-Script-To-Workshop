using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.OverwatchParser
{
    public class IncorrectElementTypeException : ArgumentException
    {
        public IncorrectElementTypeException(string paramName, bool needsToBeValue) : base(needsToBeValue ? $"{paramName} is an action, not a value." : $"{paramName} is a value, not an action.", paramName) { }
    }
}
