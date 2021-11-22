using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Generics;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;

namespace DS.Analysis.Structure.TypeAlias
{
    class DeclaredTypeAlias : AbstractDeclaredElement
    {
        readonly ITypeAliasProvider content;
        readonly IDisposableTypeDirector typeReference;
        readonly AliasProvider aliasProvider;


        public DeclaredTypeAlias(ContextInfo context, ITypeAliasProvider content)
        {
            this.content = content;

            // Setup
            var setup = content.Setup(context);
            Name = setup.Name;
            typeReference = setup.TypeReference;

            aliasProvider = new AliasProvider(Name, setup.TypeArgs, typeReference);
        }

        public override ScopedElement MakeScopedElement(ScopedElementParameters parameters)
        {
            string name = parameters.Alias ?? Name;
            return new ScopedElement(name, ScopedElementData.Create(name, aliasProvider, null));
        }

        public override void Dispose()
        {
            typeReference.Dispose();
        }


        class AliasProvider : CodeTypeProvider
        {
            readonly ITypeDirector aliasing;


            public AliasProvider(string name, TypeArgCollection typeArgs, ITypeDirector aliasing) : base(name, typeArgs)
            {
                this.aliasing = aliasing;
            }


            public override IDisposable CreateInstance(IObserver<CodeType> observer, params CodeType[] typeArgs)
            {
                var aliasDirector = new AliasDirector(this, typeArgs);
                aliasDirector.Subscribe(observer);
                return aliasDirector;
            }


            class AliasDirector : ITypeDirector, IDisposable
            {
                readonly ValueObserverCollection<CodeType> observers;
                readonly IDisposable referenceSubscription;

                public AliasDirector(AliasProvider provider, CodeType[] typeArgs)
                {
                    observers = new ValueObserverCollection<CodeType>();
                    referenceSubscription = provider.aliasing.Subscribe(type =>
                    {
                        // todo: substitute the type
                        observers.Set(type);
                    });
                }

                public void Dispose()
                {
                    observers.Complete();
                    referenceSubscription.Dispose();
                }

                public IDisposable Subscribe(IObserver<CodeType> observer) => observers.Add(observer);
            }
        }
    }
}