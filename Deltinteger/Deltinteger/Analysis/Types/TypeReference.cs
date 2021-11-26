using System;
using System.Linq;
using System.Reactive;
using DS.Analysis.Utility;
using DS.Analysis.Scopes;
using DS.Analysis.Types.Standard;

namespace DS.Analysis.Types
{
    // Represents a data type identified in the script.
    abstract class TypeReference : IDisposableTypeDirector
    {
        // The current CodeType value that the TypeReference is pointing to.
        protected CodeType CodeType;

        // Clients watching the CodeType value.
        readonly ObserverCollection<CodeType> observers = new ObserverCollection<CodeType>();

        // The TypeReferences for the type arguments.
        readonly IDisposableTypeDirector[] typeArgReferences;

        // The subscriptions to the type args. The values obtained from the observable is fed directly into the TypeArg array.
        readonly IDisposable[] typeArgSubscriptions;

        protected CodeType[] TypeArgs { get; private set; }

        protected TypeReference(IDisposableTypeDirector[] typeArgReferences)
        {
            this.typeArgReferences = typeArgReferences ?? new IDisposableTypeDirector[0];
            TypeArgs = new CodeType[this.typeArgReferences.Length];
            typeArgSubscriptions = new IDisposable[this.typeArgReferences.Length];

            // Watch type arg updates.
            for (int i = 0; i < this.typeArgReferences.Length; i++)
            {
                int captureIndex = i;

                typeArgSubscriptions[i] = this.typeArgReferences[i].Subscribe(value =>
                {
                    // Type arg changed
                    TypeArgs[captureIndex] = value;

                    // Update CodeType and flag observers.
                    Update();
                });
            }
        }

        // Sets observers.
        protected abstract void Update();

        protected void Set(CodeType value) => observers.Set(value);

        // Subscribes to the TypeReference.
        public IDisposable Subscribe(IObserver<CodeType> observer) => observers.Add(observer);


        public virtual void Dispose()
        {
            observers.Complete();

            // Dispose the subscriptions to the type args.
            foreach (var sub in typeArgSubscriptions)
                sub.Dispose();

            // Dispose the type args.
            foreach (var typeArgReference in typeArgReferences)
                typeArgReference.Dispose();
        }
    }

    class IdentifierTypeReference : TypeReference
    {
        readonly ScopeWatcher identifier;
        readonly ITypeIdentifierErrorHandler errorHandler;
        CodeTypeProvider codeTypeProvider;
        IDisposable providerSubscription;
        bool readyToUpdate = false;

        public IdentifierTypeReference(string typeName, ITypeIdentifierErrorHandler errorHandler, ScopeWatcher identifier, IDisposableTypeDirector[] generics) : base(generics)
        {
            this.identifier = identifier;
            this.errorHandler = errorHandler;

            // The IDisposable created here will be not be needed since ScopeWatcher.Dispose will handle it.
            identifier.Subscribe(nextValue =>
            {
                codeTypeProvider = SelectCodeTypeProvider(nextValue.FoundElements, typeName);
                readyToUpdate = true;
                Update();
            });
        }

        CodeTypeProvider SelectCodeTypeProvider(ScopedElementData[] scopedElements, string name)
        {
            foreach (var element in scopedElements.Where(e => e.IsMatch(name)))
            {
                var provider = element.GetCodeTypeProvider();
                if (provider != null)
                {
                    // Check type args
                    if (TypeArgs.Length != provider.Generics.Count)
                        errorHandler.GenericCountMismatch(name, provider.Generics.Count);
                    else
                        errorHandler.Success();
                    return provider;
                }
            }

            errorHandler.NoTypesMatchName();
            return StandardTypes.Unknown;
        }

        protected override void Update()
        {
            if (!readyToUpdate) return;

            providerSubscription?.Dispose();
            providerSubscription = codeTypeProvider.CreateInstance(Observer.Create<CodeType>(Set), TypeArgs);
        }

        public override void Dispose()
        {
            base.Dispose();
            identifier.Dispose();
            errorHandler.Dispose();
            providerSubscription.Dispose();
        }
    }

    interface ITypeIdentifierErrorHandler : IDisposable
    {
        void Success();
        void NoTypesMatchName();
        void GenericCountMismatch(string typeName, int expected);
    }
}