using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Expressions
{
    abstract class Expression : Node
    {
        public ITypeDirector Type { get; protected set; }

        public IObservable<Scope> Scope { get; protected set; }


        protected Expression()
        {
            Scope = new DefaultScopeSource(this);
        }


        // DefaultScopeSource gets the scope from the Type of the Expression.
        class DefaultScopeSource : IObservable<Scope>
        {
            readonly Expression expression;
            public DefaultScopeSource(Expression expression) => this.expression = expression;
            public IDisposable Subscribe(IObserver<Scope> observer) => expression.Type.Subscribe(
                onNext: codeType => observer.OnNext(codeType.Scope),
                onError: exception => observer.OnError(exception),
                onCompleted: () => observer.OnCompleted()
            );
        }
    }

    class TypeScopeObservable : IObservable<Scope>
    {
        readonly ITypeDirector typeDirector;
        public TypeScopeObservable(ITypeDirector typeDirector) => this.typeDirector = typeDirector;
        public IDisposable Subscribe(IObserver<Scope> observer) => typeDirector.Subscribe(
            onNext: codeType => observer.OnNext(codeType.Scope),
            onError: exception => observer.OnError(exception),
            onCompleted: () => observer.OnCompleted()
        );
    }
}