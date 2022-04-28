namespace DS.Analysis.Expressions
{
    using Core;
    using Types;
    using Types.Semantics;
    using Variables;
    using Methods;
    using Scopes;

    abstract class Expression : PhysicalObject, IExpressionHost
    {
        protected Expression(ContextInfo context) : base(context) { }

        /// <summary>The type of the expression.</summary>
        public PhysicalType Type
        {
            get => _type;
            protected set
            {
                _type = value ?? StandardType.Unknown.Instance;
                MarkDependentsAsStale();
            }
        }

        /// <summary>The scope of the expression. Will usually be the same as Type.Content.Scope.</summary>
        public IScopeSource ScopeSource
        {
            get => _scope ?? _type.Type.Content.ScopeSource;
            protected set
            {
                _scope = value;
                MarkDependentsAsStale();
            }
        }

        /// <summary>The method group that the expression points to. May be null.</summary>
        public MethodGroup MethodGroup
        {
            get => _methodGroup;
            protected set
            {
                _methodGroup = value;
                MarkDependentsAsStale();
            }
        }

        /// <summary>The variable that the expression points to. May be null.</summary>
        public VariableExpressionData Variable
        {
            get => _variable;
            protected set
            {
                _variable = value;
                MarkDependentsAsStale();
            }
        }

        // Backing variables
        PhysicalType _type;
        IScopeSource _scope;
        MethodGroup _methodGroup;
        VariableExpressionData _variable;

        protected void CopyStateOf(IExpressionHost other)
        {
            _type = other.Type;
            _scope = other.ScopeSource;
            _methodGroup = other.MethodGroup;
            _variable = other.Variable;
            MarkDependentsAsStale();
        }
    }
}