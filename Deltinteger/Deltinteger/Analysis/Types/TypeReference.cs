using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;
using DS.Analysis.Scopes;
using DS.Analysis.Types.Standard;

namespace DS.Analysis.Types
{
    // Represents a data type identified in the script.
    abstract class TypeReference : ITypeDirector, IDisposable
    {
        // The current CodeType value that the TypeReference is pointing to.
        protected CodeType CodeType;

        // Clients watching the CodeType value.
        readonly ObserverCollection<CodeType> observers = new ObserverCollection<CodeType>();

        // The TypeReferences for the type arguments.
        readonly TypeReference[] typeArgReferences;

        // The subscriptions to the type args. The values obtained from the observable is fed directly into the TypeArg array.
        readonly IDisposable[] typeArgSubscriptions;

        protected CodeType[] TypeArgs { get; private set; }

        protected TypeReference(TypeReference[] typeArgReferences)
        {
            this.typeArgReferences = typeArgReferences ?? new TypeReference[0];
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
                    CodeType = GetCodeType();
                    Update();
                });
            }
        }

        protected abstract CodeType GetCodeType();

        // Sets observers.
        protected virtual void Update() => observers.Set(CodeType);

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

        public IdentifierTypeReference(string typeName, ITypeIdentifierErrorHandler errorHandler, ScopeWatcher identifier, TypeReference[] generics) : base(generics)
        {
            this.identifier = identifier;
            this.errorHandler = errorHandler;

            // The IDisposable created here will be not be needed since ScopeWatcher.Dispose will handle it.
            identifier.Subscribe(nextValue =>
            {
                codeTypeProvider = SelectCodeTypeProvider(nextValue.FoundElements, typeName);

                // Update
                CodeType = GetCodeType();
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

        protected override CodeType GetCodeType() => codeTypeProvider.CreateInstance(TypeArgs);

        public override void Dispose()
        {
            base.Dispose();
            identifier.Dispose();
            errorHandler.Dispose();
        }
    }

    interface ITypeIdentifierErrorHandler : IDisposable
    {
        void Success();
        void NoTypesMatchName();
        void GenericCountMismatch(string typeName, int expected);
    }
}