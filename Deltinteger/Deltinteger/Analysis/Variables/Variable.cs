using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;
using DS.Analysis.Expressions;
using DS.Analysis.Expressions.Identifiers;

namespace DS.Analysis.Variables
{
    class Variable : IIdentifierHandler
    {
        public VariableProvider VariableProvider { get; }
        public ITypeDirector Type { get; }

        public Variable(VariableProvider variableProvider)
        {
            VariableProvider = variableProvider;
        }


        // IIdentifierHandler
        ITypeDirector IIdentifierHandler.GetTypeDirector() => Type;
        IObservable<Scope> IIdentifierHandler.GetScopeDirector() => new TypeScopeObservable(Type);
    }
}