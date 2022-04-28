namespace DS.Analysis.Scopes.Selector
{
    using Methods;
    using Expressions.Identifiers;
    using Types;

    /// <summary>
    /// Contains data about an identifier chosen via filtering the current scope for a named element.
    /// This data isn't exposed directly in ScopedElement so that scoped methods can be batched into method groups.
    /// </summary>
    class IdentifiedElement
    {
        /// <summary>The element's variable data. May be null.</summary>
        public IdentifierInfo IdentifierHandler { get; init; }

        /// <summary>Handles the element's usage as a type. May be null.</summary>
        public ITypeNodeManager TypePartHandler { get; init; }

        /// <summary>The method group that the variable points to. May be null.</summary>
        public MethodGroup MethodGroup { get; init; }


        public IdentifiedElement(IdentifierInfo identifierHandler, ITypeNodeManager typePartHandler, MethodGroup methodGroup)
        {
            IdentifierHandler = identifierHandler;
            TypePartHandler = typePartHandler;
            MethodGroup = methodGroup;
        }

        /// <summary>An identified variable.</summary>
        public IdentifiedElement(IdentifierInfo identifierHandler)
        {
            IdentifierHandler = identifierHandler;
        }

        /// <summary>An identified type or module.</summary>
        public IdentifiedElement(ITypeNodeManager typePartHandler)
        {
            TypePartHandler = typePartHandler;
        }

        /// <summary>An identified method group.</summary>
        public IdentifiedElement(MethodGroup methodGroup)
        {
            TypePartHandler = StandardType.MethodGroup.Provider;
            MethodGroup = methodGroup;
        }


        public static readonly IdentifiedElement Unknown = new IdentifiedElement(IdentifierInfo.Unknown, StandardType.Unknown.Provider, null);
    }
}