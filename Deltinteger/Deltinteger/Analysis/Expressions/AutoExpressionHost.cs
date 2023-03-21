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
            get => _scope ?? _type?.Type.Content.ScopeSource ?? (IScopeSource)EmptyScopeSource.Instance;
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
        readonly SingleNode singleNode;


        public AutoExpressionHost(SingleNode singleNode)
        {
            this.singleNode = singleNode;
        }


        // Called when an expression attribute is changed.
        private void Update() => singleNode.MakeDependentsStale();


        public IDisposable AddDisposable(IDisposable disposable) => singleNode.AddDisposable(disposable);
        public void DisposeOnUpdate(IDisposable disposable) => singleNode.DisposeOnUpdate(disposable);

        public void DependOn(IDependable dependable) => singleNode.DependOn(dependable);
        public void DependOnUntilUpdate(IDependable dependable) => singleNode.DisposeOnUpdate(singleNode.DependOn(dependable));


        // IDependable
        public IDisposable AddDependent(IDependent dependent) => singleNode.AddDependent(dependent);

        // IDisposable
        public void Dispose() => singleNode.Dispose();
    }
}