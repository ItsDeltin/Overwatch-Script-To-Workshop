using System;
using DS.Analysis.Expressions;
using DS.Analysis.Types;

namespace DS.Analysis.Scopes
{
    class ScopedElement
    {
        public string Alias { get; }

        public ScopedElement(string alias)
        {
            Alias = alias;
        }

        public virtual CodeTypeProvider GetCodeTypeProvider() => null;
        public virtual IIdentifierHandler GetIdentifierHandler() => null;
    }
}