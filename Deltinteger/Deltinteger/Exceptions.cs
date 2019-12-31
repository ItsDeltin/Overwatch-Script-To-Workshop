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
        public SyntaxErrorException(string message) : base(message)
        {
        }
        public SyntaxErrorException() : base()
        {
        }
    }

    class StringParseFailedException : Exception
    {
        public int StringIndex { get; } = -1;
        public int Length { get; } = 0;

        public StringParseFailedException(string message) : base(message) {}
        public StringParseFailedException(string message, int index, int length) : this(message)
        {
            StringIndex = index;
            Length = length;
        }
    }
}
