namespace DS.Analysis.Expressions
{
    using Core;
    using Types;
    using Types.Semantics;
    using Types.Standard;
    using Variables;
    using Methods;
    using Scopes;

    abstract class Expression : PhysicalObject
    {
        protected Expression(ContextInfo context) : base(context) { }

        /// <summary>The type of the expression.</summary>
        public PhysicalType PhysicalType
        {
            get => _type;
            protected set
            {
                _type = value ?? StandardTypes.Unknown.Instance;
                MarkDependentsAsStale();
            }
        }

        /// <summary>The scope of the expression. Will usually be the same as Type.Content.Scope.</summary>
        public Scope Scope
        {
            get => _scope ?? new Scope(_type.Type.Content.ScopeSource);
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
        Scope _scope;
        MethodGroup _methodGroup;
        VariableExpressionData _variable;

        protected void CopyStateOf(Expression other)
        {
            _type = other._type;
            _scope = other._scope;
            _methodGroup = other._methodGroup;
            _variable = other._variable;
            MarkDependentsAsStale();
        }
    }
}