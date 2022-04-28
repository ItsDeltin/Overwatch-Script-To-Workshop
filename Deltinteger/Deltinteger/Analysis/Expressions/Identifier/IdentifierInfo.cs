using System;
using DS.Analysis.Scopes;
using DS.Analysis.Types;

namespace DS.Analysis.Expressions.Identifiers
{
    struct IdentifierInfo
    {
        public readonly ITypeDirector TypeDirector;

        public IdentifierInfo(ITypeDirector typeDirector)
        {
            TypeDirector = typeDirector;
        }

        public static readonly IdentifierInfo Unknown = new IdentifierInfo(Types.StandardType.Unknown.Director);
    }
}