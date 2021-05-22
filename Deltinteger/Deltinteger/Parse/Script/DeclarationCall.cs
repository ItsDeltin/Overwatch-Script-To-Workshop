using System;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    // Represents a call to a declared script element.
    public struct DeclarationCall
    {
        // The full range of the call. Ex: ['new MyClass()']
        public DocRange CallRange { get; }

        public DeclarationCall(DocRange callRange)
        {
            // callRange is required.
            CallRange = callRange ?? throw new ArgumentNullException(nameof(callRange));
        }
    }
}