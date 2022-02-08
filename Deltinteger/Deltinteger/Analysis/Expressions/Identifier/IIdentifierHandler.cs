using System;
using DS.Analysis.Scopes;
using DS.Analysis.Types;

namespace DS.Analysis.Expressions.Identifiers
{
    interface IIdentifierHandler
    {
        ITypeDirector TypeDirector { get; }
        IObservable<Scope> GetScopeDirector();
    }
}