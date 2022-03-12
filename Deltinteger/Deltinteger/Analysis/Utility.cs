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

        public static IDependent CreateDependent(IMaster master, Action update) => new AnonymousDependent(master, update);


        public static ICodeTypeProvider CreateCodeTypeProvider(string name,
                TypeArgCollection generics,
                IGetIdentifier getIdentifier,
                Func<ProviderArguments, IDisposableTypeDirector> instanceFactory)
                => new AnonymousCodeTypeProvider(name, generics, getIdentifier, instanceFactory);


        public static IDisposableTypeDirector CreateTypeDirector(DirectorFactory directorFactory)
        {
            var disposable = new SerialDisposable();

            // Create director
            var director = new SerialDisposableTypeDirector(disposable);

            // Create director from type arguments
            disposable.Disposable = directorFactory(type => director.Type = type);

            return director;
        }

        public static ITypePartHandler CreateTypePartHandler(IsTypePartValid isValid, GetTypePartInfo getInfo) =>
            new AnonymousTypePartHandler(isValid, getInfo);


        // Used to construct directors.
        public delegate IDisposable DirectorFactory(SetDirectorType setDirectorType);
        // Used inside the DirectorFactory to set the serial director's type.
        public delegate void SetDirectorType(CodeType type);

        // ITypePartHandler.IsValid
        public delegate bool IsTypePartValid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount);
        // ITypePartHandler.GetTypeInfo
        public delegate TypePartInfoResult GetTypePartInfo(ProviderArguments arguments);


        class GenericUpdatable : IUpdatable
        {
            readonly Action action;
            public GenericUpdatable(Action action) => this.action = action;
            public void Update() => action();
        }

        class AnonymousDependent : IDependent, IUpdatable
        {
            readonly IMaster master;
            readonly Action update;

            public AnonymousDependent(IMaster master, Action update) => (this.master, this.update) = (master, update);

            public void MarkAsStale()
            {
                master.AddStaleObject(this);
            }

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
                    errorHandler.GenericCountMismatch(this, Generics.Count);
                return true;
            }

            public TypePartInfoResult GetPartInfo(ProviderArguments arguments)
            {
                var disposableDirector = CreateInstance(arguments);
                return new TypePartInfoResult(
                    new TypeDirectorPartInfo(disposableDirector),
                    disposableDirector
                );
            }
        }

        class TypeDirectorPartInfo : ITypePartInfo
        {
            public IScopeSource ScopeSource => director.Type.Content.ScopeSource;
            public CodeType Type => director.Type;
            public IParentElement ParentElement =>;

            readonly ITypeDirector director;

            public TypeDirectorPartInfo(ITypeDirector director) => this.director = director;

            public IDisposable AddDependent(IDependent dependent) => director.AddDependent(dependent);
        }

        class AnonymousTypePartHandler : ITypePartHandler
        {
            readonly IsTypePartValid isValid;
            readonly GetTypePartInfo getInfo;

            public AnonymousTypePartHandler(IsTypePartValid isValid, GetTypePartInfo getInfo)
            {
                this.isValid = isValid;
                this.getInfo = getInfo;
            }

            public TypePartInfoResult GetPartInfo(ProviderArguments arguments) => getInfo(arguments);

            public bool Valid(ITypeIdentifierErrorHandler errorHandler, int typeArgCount) => isValid(errorHandler, typeArgCount);
        }
    }
}