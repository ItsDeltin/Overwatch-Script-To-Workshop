using System;
using DS.Analysis.Expressions.Identifiers;
using DS.Analysis.Types;
using DS.Analysis.Types.Standard;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopedElement
    {
        public string Name { get; }

        public virtual CodeTypeProvider Provider { get; protected set; }
        public virtual IIdentifierHandler IdentifierHandler { get; protected set; }
        public virtual ITypePartHandler TypePartHandler { get; protected set; }


        public ScopedElement(string alias)
        {
            Name = alias;
        }


        public override string ToString() => Name;

        public static ScopedElement Create(string name, CodeTypeProvider provider, IIdentifierHandler identifierHandler, ITypePartHandler typePartHandler = null) => new ScopedElement(name)
        {
            Provider = provider,
            IdentifierHandler = identifierHandler,
            TypePartHandler = typePartHandler
        };

        public static ScopedElement Unknown(string name) => Create(name, StandardTypes.Unknown, UnknownIdentifierHandler.Instance);
    }
}