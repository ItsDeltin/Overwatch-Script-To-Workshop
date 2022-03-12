using System;

namespace DS.Analysis.Expressions
{
    using DS.Analysis.Core;
    using DS.Analysis.Methods;
    using DS.Analysis.Scopes;
    using DS.Analysis.Types.Semantics;

    sealed class AutoExpressionHost : IExpressionHost
    {
        // Public accessors
        public PhysicalType Type
        {
            get => _type;
            set
            {
                _type = value;
                Update();
            }
        }

        public IScopeSource ScopeSource
        {
            get => _scope ?? _type.Type.Content.ScopeSource;
            set
            {
                _scope = value;
                Update();
            }
        }

        public MethodGroup MethodGroup
        {
            get => _methodGroup;
            set
            {
                _methodGroup = value;
                Update();
            }
        }

        public VariableExpressionData Variable
        {
            get => _variable;
            set
            {
                _variable = value;
                Update();
            }
        }


        // Backing variables
        PhysicalType _type;
        IScopeSource _scope;
        MethodGroup _methodGroup;
        VariableExpressionData _variable;


        // Operators
        public DependencyHandler DependencyHandler { get; }


        public AutoExpressionHost(DependencyHandler dependencyHandler)
        {
            this.DependencyHandler = dependencyHandler;
        }


        // Called when an expression attribute is changed.
        private void Update() => DependencyHandler.MakeDependentsStale();


        public T AddDisposable<T>(T disposable, DisposableLifetime lifetime = default(DisposableLifetime)) where T : IDisposable
            => DependencyHandler.AddDisposable(disposable);

        public void DependOn(IDependable dependable, DisposableLifetime lifetime = default(DisposableLifetime))
            => DependencyHandler.DependOn(dependable);


        // IDependable
        public IDisposable AddDependent(IDependent dependent) => DependencyHandler.AddDependent(dependent);

        // IDisposable
        public void Dispose() => DependencyHandler.Dispose();
    }
}