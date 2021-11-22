namespace DS.Analysis.Types.Standard
{
    using Scopes;

    static class StandardTypes
    {
        // Unknown
        public static readonly SingletonCodeTypeProvider Unknown = Create("?");

        // Number
        public static readonly SingletonCodeTypeProvider Number = Create("Number");

        // Scope source
        public static readonly IScopeSource StandardSource;


        static StandardTypes()
        {
            ScopeSource standardSource = new ScopeSource();
            standardSource.AddScopedElement(Number.ScopedElement);

            StandardSource = standardSource;
        }


        static SingletonCodeTypeProvider Create(string name) => new SingletonCodeTypeProvider(name);
    }
}