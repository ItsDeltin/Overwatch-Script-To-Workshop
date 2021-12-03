using System;

namespace DS.Analysis.Types.Semantics
{
    interface ITypeIdentifierErrorHandler : IDisposable
    {
        void Clear();
        void NoTypesMatchName();
        void GenericCountMismatch(string typeName, int expected);
        void ModuleHasTypeArgs();
    }
}