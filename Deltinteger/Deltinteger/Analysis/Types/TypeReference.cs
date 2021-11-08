using System;
using System.Collections.Generic;
using DS.Analysis.Utility;
using DS.Analysis.Scopes;
using DS.Analysis.Types.Standard;

namespace DS.Analysis.Types
{
    // Represents a data type identified in the script.
    abstract class TypeReference : ITypeDirector, IDisposable
    {
        protected CodeType CodeType;

        readonly TypeReference[] _typeArgReferences;

        readonly ObserverCollection<CodeType> _observers = new ObserverCollection<CodeType>();
        readonly List<IDisposable> _genericWatchers = new List<IDisposable>();

        protected CodeType[] TypeArgs { get; private set; }

        protected TypeReference(TypeReference[] generics)
        {
            _typeArgReferences = generics ?? new TypeReference[0];
            TypeArgs = new CodeType[_typeArgReferences.Length];

            // Watch type arg updates.
            for (int i = 0; i < _typeArgReferences.Length; i++)
            {
                int captureIndex = i;

                _genericWatchers.Add(_typeArgReferences[i].Subscribe(value => {
                    // Type arg changed
                    TypeArgs[captureIndex] = value;

                    // Update CodeType and flag observers.
                    CodeType = GetCodeType();
                    Update();
                }));
            }
        }

        protected abstract CodeType GetCodeType();

        // Sets observers.
        protected virtual void Update() => _observers.Set(CodeType);

        // Subscribes to the TypeReference.
        public IDisposable Subscribe(IObserver<CodeType> observer) => _observers.Add(observer);


        public ITypeDirector GetDirector() => this;

        public virtual void Dispose()
        {
            _observers.Complete();
            foreach (var genericWatcher in _genericWatchers)
                genericWatcher.Dispose();
        }
    }

    class IdentifierTypeReference : TypeReference
    {
        readonly ScopeWatcher _identifier;
        readonly ITypeIdentifierErrorHandler _errorHandler;
        CodeTypeProvider _codeTypeProvider;

        public IdentifierTypeReference(string typeName, ITypeIdentifierErrorHandler errorHandler, ScopeWatcher identifier, TypeReference[] generics) : base(generics)
        {
            _identifier = identifier;
            _errorHandler = errorHandler;

            // The IDisposable created here will be not be needed since ScopeWatcher.Dispose will handle it.
            identifier.Subscribe(nextValue => {
                // todo: generic filter
                _codeTypeProvider = SelectCodeTypeProvider(nextValue.FoundElements, typeName);

                // Update
                CodeType = GetCodeType();
                Update();
            });
        }

        CodeTypeProvider SelectCodeTypeProvider(ScopedElementData[] scopedElements, string name)
        {
            foreach (var element in scopedElements)
            {
                var provider = element.GetCodeTypeProvider();
                if (provider != null && provider.IsMatch(name))
                {
                    _errorHandler.Success();
                    return provider;
                }
            }

            _errorHandler.NoTypesMatchName();
            return StandardTypeProviders.Unknown;
        }

        protected override CodeType GetCodeType() => _codeTypeProvider.CreateInstance(TypeArgs);

        public override void Dispose()
        {
            base.Dispose();
            _identifier.Dispose();
            _errorHandler.Dispose();
        }
    }

    interface ITypeIdentifierErrorHandler : IDisposable
    {
        void Success();
        void NoTypesMatchName();
        void GenericCountMismatch();
    }
}