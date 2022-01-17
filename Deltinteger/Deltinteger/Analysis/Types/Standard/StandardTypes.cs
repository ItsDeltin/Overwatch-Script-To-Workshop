namespace DS.Analysis.Types.Standard
{
    using Scopes;

    static class StandardTypes
    {
        // Unknown
        public static readonly SingletonCodeTypeProvider Unknown = Create("?");

        // Method group
        public static readonly SingletonCodeTypeProvider MethodGroup = Create("method group");

        // Void
        public static readonly SingletonCodeTypeProvider Void = Create("void");

        // Number
        public static readonly SingletonCodeTypeProvider Number = Create("Number");

        // Scope source
        public static readonly IScopeSource StandardSource;


        static StandardTypes()
        {
            ScopeSource standardSource = new ScopeSource();
            StandardSource = standardSource;

            // Add types to source.
            standardSource.AddScopedElement(Number.ScopedElement);
        }


        static SingletonCodeTypeProvider Create(string name) => new SingletonCodeTypeProvider(name);
    }
}