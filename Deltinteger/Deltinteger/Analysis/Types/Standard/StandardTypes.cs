using System.Reactive.Disposables;

namespace DS.Analysis.Types
{
    using Scopes;

    class StandardType
    {
        // Unknown
        public static readonly StandardType Unknown = Create("?");

        // Method group
        public static readonly StandardType MethodGroup = Create("method group");

        // Void
        public static readonly StandardType Void = Create("void");

        // Number
        public static readonly StandardType Number = Create("Number");

        // Scope source
        public static readonly IScopeSource StandardSource;


        static StandardType()
        {
            ScopeSource standardSource = new ScopeSource();
            StandardSource = standardSource;

            // Add types to source.
            standardSource.AddScopedElement(Number.Provider.CreateScopedElement());
        }

        static StandardType Create(string name) => new StandardType(name);



        public CodeType Instance { get; }
        public IDisposableTypeDirector Director { get; }
        public ICodeTypeProvider Provider { get; }
        readonly string name;

        public StandardType(string name)
        {
            this.name = name;
            var identifier = new UniversalIdentifier(name);

            Instance = CodeType.Create(Components.CodeTypeContent.Empty, new StandardTypeComparison(this), identifier);
            Director = Utility2.CreateDirector(setType =>
            {
                setType(Instance);
                return Disposable.Empty;
            });

            Provider = Utility2.CreateProvider(name, Generics.TypeArgCollection.Empty, identifier, args => Director);
        }

        class StandardTypeComparison : Types.Components.ITypeComparison
        {
            readonly StandardType standardType;

            public StandardTypeComparison(StandardType standardType)
            {
                this.standardType = standardType;
            }

            public bool CanBeAssignedTo(CodeType other) => Implements(other);
            public bool Implements(CodeType other) => other == standardType.Instance;
            public bool Is(CodeType other) => other == standardType.Instance;
            public int GetTypeHashCode() => standardType.GetHashCode();
        }
    }
}