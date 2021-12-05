using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;

namespace DS.Analysis.Expressions
{
    abstract class Expression : Node, IObservable<ExpressionData>
    {
        protected ObserverCollection<ExpressionData> Observers { get; } = new ValueObserverCollection<ExpressionData>();

        public ITypeDirector Type { get; }


        protected Expression()
        {
            Type = new ExpressionTypeDirector(this);
        }


        protected void SetTypeDirector(ITypeDirector director)
        {
            AddDisposable(director.Subscribe(type => Observers.Set(new ExpressionData(type))));
        }

        public IDisposable Subscribe(IObserver<ExpressionData> observer) => Observers.Add(observer);


        class ExpressionTypeDirector : ITypeDirector
        {
            readonly Expression expression;
            public ExpressionTypeDirector(Expression expression) => this.expression = expression;
            public IDisposable Subscribe(IObserver<CodeType> observer) => expression.Observers.Select(observer, expressionData => expressionData.Type);
        }
    }

    class TypeScopeObservable : IObservable<Scope>
    {
        readonly ITypeDirector typeDirector;
        public TypeScopeObservable(ITypeDirector typeDirector) => this.typeDirector = typeDirector ?? throw new ArgumentNullException(nameof(typeDirector));
        public IDisposable Subscribe(IObserver<Scope> observer) => typeDirector.Select(observer, type => new Scope(type.Content.ScopeSource));
    }
}