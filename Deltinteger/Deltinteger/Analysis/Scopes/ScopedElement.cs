using System;
using DS.Analysis.Expressions;
using DS.Analysis.Types;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    class ScopedElement : IObservable<ScopedElementData>
    {
        public string Alias { get; }
        protected ValueObserverCollection<ScopedElementData> Observers { get; }

        public ScopedElement(string alias) : this(alias, ScopedElementData.Unknown) {}

        public ScopedElement(string alias, ScopedElementData scopedElementData)
        {
            Alias = alias;
            Observers = new ValueObserverCollection<ScopedElementData>(scopedElementData);
        }

        public IDisposable Subscribe(IObserver<ScopedElementData> observer) => Observers.Add(observer);

        public override string ToString() => Alias;
    }

    class ScopedElementData
    {
        // TODO: give Unknown default CodeTypeProvider and IdentfierHandler.
        public static readonly ScopedElementData Unknown = new ScopedElementData();

        public virtual CodeTypeProvider GetCodeTypeProvider() => null;
        public virtual IIdentifierHandler GetIdentifierHandler() => null;

        public virtual bool IsMatch(string name) => false;
    }
}