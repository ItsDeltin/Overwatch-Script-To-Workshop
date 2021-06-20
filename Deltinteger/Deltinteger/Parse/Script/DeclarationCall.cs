using System;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public interface IDeclarationKey
    {
        string Name { get; }
    }

    // Represents a call to a declared script element.
    public struct DeclarationCall
    {
        // The full range of the call. Ex: ['new MyClass()']
        public DocRange CallRange { get; }
        public bool IsDeclaration { get; }

        public DeclarationCall(DocRange callRange, bool isDeclaration)
        {
            // callRange is required.
            CallRange = callRange ?? throw new ArgumentNullException(nameof(callRange));
            IsDeclaration = isDeclaration;
        }
    }
}