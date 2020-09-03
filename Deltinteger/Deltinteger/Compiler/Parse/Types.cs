using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class TokenCapture
    {
        public int StartToken { get; }
        public int Length { get; private set; }
        public object Node { get; private set; }

        public TokenCapture(int startToken)
        {
            StartToken = startToken;
        }

        public void Finish(int position, object node)
        {
            Length = position - StartToken;
            Node = node;
        }

        public bool IsValid => Length > 0;
    }
}