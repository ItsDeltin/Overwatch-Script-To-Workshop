namespace DS.Analysis.Types
{
    using System;
    using System.Reactive;
    using System.Reactive.Disposables;
    using Generics;
    using Utility;
    using Scopes;

    /// <summary>Contains the metadata of a datatype. Can be used to instantiate type instances.</summary>
    class CodeTypeProvider
    {
        public string Name { get; }
        public TypeArgCollection Generics { get; }

        public CodeTypeProvider(string name)
        {
            Name = name;
            Generics = TypeArgCollection.Empty;
        }

        public CodeTypeProvider(string name, TypeArgCollection typeArgCollection)
        {
            Name = name;
            Generics = typeArgCollection;
        }

        /// <summary>
        /// Creates a data type instance. The actual value is broadcasted through the <paramref name="observer"/> argument.
        /// </summary>
        /// <param name="observer">The observer where the CodeType instance is broadcasted to. This will be called again when the CodeType content changes.</param>
        /// <param name="typeArgs">The type arguments for the CodeType instance based off the generics of the provider.</param>
        /// <returns>An IDisposable which cleans up the instance provider.</returns>
        public virtual IDisposable CreateInstance(IObserver<CodeType> observer, params CodeType[] typeArgs)
        {
            observer.OnNext(new CodeType());
            return Disposable.Empty;
        }

        /// <summary>
        /// Creates a proper ITypeDirector from the CreateInstance implementation.
        /// This should be reserved for universal/standard type providers which need a convenient ITypeDirector pointer.
        /// </summary>
        /// <param name="typeArgs">The type arguments for the CodeType instance based off the generics of the provider.</param>
        /// <returns>An InstanceTypeDirector which can be used as a type director and can be disposed to clean up the reference
        /// to the original CodeTypeProvider (this).</returns>
        public IDisposableTypeDirector CreateInstance(params CodeType[] typeArgs) => new InstanceTypeDirector(this, typeArgs);

        /// <summary>Creates a ScopedElement from the provider.</summary>
        /// <returns>A new ScopedElement with the name and provider fulfilled from this provider.</returns>
        public virtual ScopedElement CreateScopedElement() => new ScopedElement(Name, ScopedElementData.Create(Name, this, null));
    }

    /// <summary>
    /// An ITypeDirector implementation linked to a CodeTypeProvider instance.
    /// </summary>
    class InstanceTypeDirector : IDisposableTypeDirector
    {
        readonly CodeTypeProvider provider;
        readonly IDisposable subscription;
        readonly ObserverCollection<CodeType> observers = new ValueObserverCollection<CodeType>();

        public InstanceTypeDirector(CodeTypeProvider provider, CodeType[] typeArgs)
        {
            this.provider = provider;
            subscription = provider.CreateInstance(Observer.Create<CodeType>(observers.Set), typeArgs);
        }

        public void Dispose() => subscription.Dispose();

        public IDisposable Subscribe(IObserver<CodeType> observer) => observers.Add(observer);
    }

    /// <summary>
    /// Represents a universal non-generic data type provider.
    /// </summary>
    class SingletonCodeTypeProvider : CodeTypeProvider
    {
        public CodeType Instance { get; }
        public ITypeDirector Director { get; }
        public ScopedElement ScopedElement { get; }

        public SingletonCodeTypeProvider(string name) : base(name)
        {
            Instance = new CodeType();
            Director = CreateInstance();
            ScopedElement = new ScopedElement(Name, ScopedElementData.Create(Name, this, null));
        }

        public override IDisposable CreateInstance(IObserver<CodeType> observer, params CodeType[] typeArgs)
        {
            observer.OnNext(Instance);
            return Disposable.Empty;
        }
    }
}