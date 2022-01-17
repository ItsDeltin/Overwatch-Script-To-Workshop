namespace DS.Analysis.Scopes.Selector
{
    using Methods;
    using Expressions.Identifiers;
    using Types.Standard;

    /// <summary>
    /// Contains data about an identifier chosen via filtering the current scope for a named element.
    /// This data isn't exposed directly in ScopedElement so that scoped methods can be batched into method groups.
    /// </summary>
    class IdentifiedElement
    {
        /// <summary>The element's variable data. May be null.</summary>
        public IIdentifierHandler IdentifierHandler { get; init; }

        /// <summary>Handles the element's usage as a type. May be null.</summary>
        public ITypePartHandler TypePartHandler { get; init; }

        /// <summary>The method group that the variable points to. May be null.</summary>
        public MethodGroup MethodGroup { get; init; }


        public IdentifiedElement(IIdentifierHandler identifierHandler, ITypePartHandler typePartHandler, MethodGroup methodGroup)
        {
            IdentifierHandler = identifierHandler;
            TypePartHandler = typePartHandler;
            MethodGroup = methodGroup;
        }

        /// <summary>An identified variable.</summary>
        public IdentifiedElement(IIdentifierHandler identifierHandler)
        {
            IdentifierHandler = identifierHandler;
        }

        /// <summary>An identified type or module.</summary>
        public IdentifiedElement(ITypePartHandler typePartHandler)
        {
            TypePartHandler = typePartHandler;
        }

        /// <summary>An identified method group.</summary>
        public IdentifiedElement(MethodGroup methodGroup)
        {
            TypePartHandler = StandardTypes.MethodGroup;
            MethodGroup = methodGroup;
        }


        public static readonly IdentifiedElement Unknown = new IdentifiedElement(UnknownIdentifierHandler.Instance, StandardTypes.Unknown, null);
    }
}