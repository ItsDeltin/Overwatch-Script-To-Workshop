using System;
using System.Reactive.Disposables;
using DS.Analysis.Core;
using DS.Analysis.Scopes;
using DS.Analysis.Types;
using DS.Analysis.Types.Generics;
using DS.Analysis.Types.Semantics;

namespace DS.Analysis
{
    using Utility;

    static class Utility2
    {
        /// <summary>Creates an IUpdatable from an action.</summary>
        /// <param name="action">The action that is executed when the IUpdatable is triggered.</param>
        /// <returns></returns>
        public static IUpdatable CreateUpdatable(Action action) => new GenericUpdatable(action);

        public static IDependent CreateDependent(IMaster master, Action update)
        {
            var dep = new AnonymousDependent(master, update);
            master.AddStaleObject(dep);
            return dep;
        }


        public static ICodeTypeProvider CreateProvider(string name,
                TypeArgCollection generics,
                IGetIdentifier getIdentifier,
                Func<ProviderArguments, IDisposableTypeDirector> instanceFactory)
                => new AnonymousCodeTypeProvider(name, generics, getIdentifier, instanceFactory);


        /// <summary>
        /// Creates a type director
        /// </summary>
        /// <param name="directorFactory">(CodeType => void) => IDisposable</param>
        /// <returns></returns>
        [System.Diagnostics.DebuggerStepThrough]
        public static IDisposableTypeDirector CreateDirector(DirectorFactory directorFactory)
        {
            var disposable = new SerialDisposable();

            // Create director
            var director = new SerialDisposableTypeDirector(disposable);

            // Create director from type arguments
            disposable.Disposable = directorFactory(type => director.Type = type);

            return director;
        }


        /// <summary>
        /// Creates a type provider and the director generator.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <param name="typeParams">The type's type parameters.</param>
        /// <param name="getIdentifier">(todo: remove?) Obtains the name of the type as represented in the current scope.</param>
        /// <param name="instanceFactory">A function that is used to set the director's type.
        /// An IDisposable can be returned which is linked to the generated type director.</param>
        /// <returns>A new ICodeTypeProvider.</returns>
        public static ICodeTypeProvider CreateProviderAndDirector(
            string name,
            TypeArgCollection typeParams,
            IGetIdentifier getIdentifier,
            Action<CreateProviderAndDirectorHelper> instanceFactory
        ) => CreateProvider(name, typeParams, getIdentifier, arguments =>
            CreateDirector(setType =>
            {
                var disposables = new DisposableCollection();
                bool createdInstance = false;

                instanceFactory(new(arguments, setType, d =>
                {
                    if (createdInstance)
                        throw new Exception("Director has already been created, shouldn't link any more disposables");

                    disposables.Add(d);
                }));

                createdInstance = true;
                return disposables;
            }));

        /// <summary>
        /// Struct used by CreateProviderWithDirector to combine the provider arguments and the director's SetType delegate.
        /// </summary>
        public struct CreateProviderAndDirectorHelper
        {
            // Public accessors to arguments' values
            public IParentElement Parent => arguments.Parent;
            public CodeType[] TypeArgs => arguments.TypeArgs;
            // setType delegate
            public void SetType(CodeType type) => setType(type);

            /// <summary>Links a disposable to the director.</summary>
            /// <param name="disposable">The item that is disposed when the director is disposed.</param>
            public void AddDisposable(IDisposable disposable) => addDisposable(disposable);

            readonly ProviderArguments arguments;
            readonly SetDirectorType setType;
            readonly Action<IDisposable> addDisposable;

            public CreateProviderAndDirectorHelper(ProviderArguments arguments, SetDirectorType setType, Action<IDisposable> addDisposable)
            {
                this.arguments = arguments;
                this.setType = setType;
                this.addDisposable = addDisposable;
            }
        }


        public static ITypeNodeManager CreateTypePartHandler(IsTypePartValid isValid, GetTypePartInfo getInfo) =>
            new AnonymousTypeNodeManager(isValid, getInfo);


        // Used to construct directors.
        public delegate IDisposable DirectorFactory(SetDirectorType setDirectorType);
        // Used inside the DirectorFactory to set the serial director's type.
        public delegate void SetDirectorType(CodeType type);

        // ITypePartHandler.IsValid
        public delegate bool IsTypePartValid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount);
        // ITypePartHandler.GetTypeInfo
        public delegate ITypeNodeInstance GetTypePartInfo(ProviderArguments arguments);


        class GenericUpdatable : IUpdatable
        {
            readonly Action action;
            public GenericUpdatable(Action action) => this.action = action;
            [System.Diagnostics.DebuggerStepThrough]
            public void Update() => action();
        }

        class AnonymousDependent : IDependent, IUpdatable
        {
            readonly IMaster master;
            readonly Action update;

            public AnonymousDependent(IMaster master, Action update) => (this.master, this.update) = (master, update);

            [System.Diagnostics.DebuggerStepThrough]
            public void MarkAsStale()
            {
                master.AddStaleObject(this);
            }

            [System.Diagnostics.DebuggerStepThrough]
            public void Update() => update();
        }

        class AnonymousCodeTypeProvider : ICodeTypeProvider
        {
            public string Name { get; }
            public TypeArgCollection Generics { get; }
            public IGetIdentifier GetIdentifier { get; }

            readonly Func<ProviderArguments, IDisposableTypeDirector> instanceFactory;

            public AnonymousCodeTypeProvider(
                string name,
                TypeArgCollection generics,
                IGetIdentifier getIdentifier,
                Func<ProviderArguments, IDisposableTypeDirector> instanceFactory
            )
            {
                (Name, Generics, GetIdentifier, this.instanceFactory) = (name, generics, getIdentifier, instanceFactory);
            }

            public IDisposableTypeDirector CreateInstance(ProviderArguments arguments) => instanceFactory(arguments);

            public bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount)
            {
                if (typeArgCount != Generics.Count)
                    errorHandler.GenericCountMismatch(GetIdentifier, Generics.Count);
                return true;
            }

            public ITypeNodeInstance GetPartInfo(ProviderArguments arguments)
            {
                var disposableDirector = CreateInstance(arguments);
                return new TypeDirectorNodeInstance(disposableDirector, arguments.Parent);
            }

            public ScopedElement CreateScopedElement()
            {
                return ScopedElement.CreateType(Name, this);
            }

            /// <summary>
            /// The AnonymousCodeTypeProvider used as a type path node.
            /// </summary>
            class TypeDirectorNodeInstance : ITypeNodeInstance
            {
                public IScopeSource ScopeSource => director.Type.Content.ScopeSource;
                public CodeType Type => director.Type;
                public IParentElement ParentElement { get; }

                readonly IDisposableTypeDirector director;

                public TypeDirectorNodeInstance(IDisposableTypeDirector director, IParentElement parent) => (this.director, this.ParentElement) = (director, parent);

                public IDisposable AddDependent(IDependent dependent) => director.AddDependent(dependent);

                public void Dispose() => director.Dispose();
            }
        }

        class AnonymousTypeNodeManager : ITypeNodeManager
        {
            readonly IsTypePartValid isValid;
            readonly GetTypePartInfo getInfo;

            public AnonymousTypeNodeManager(IsTypePartValid isValid, GetTypePartInfo getInfo)
            {
                this.isValid = isValid;
                this.getInfo = getInfo;
            }

            public ITypeNodeInstance GetPartInfo(ProviderArguments arguments) => getInfo(arguments);

            public bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount) => isValid(errorHandler, typeArgCount);
        }
    }
}