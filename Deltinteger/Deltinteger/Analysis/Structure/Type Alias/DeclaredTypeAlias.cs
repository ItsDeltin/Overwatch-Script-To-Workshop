using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Generics;
using DS.Analysis.Types.Components;
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

        public override void AddToScope(IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(ScopedElement.Create(Name, aliasProvider, null));
        }

        public override void AddToContent(TypeContentBuilder contentBuilder) => contentBuilder.AddElement(new ProviderTypeElement(aliasProvider));


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


            public override IDisposable CreateInstance(IObserver<CodeType> observer, ProviderArguments arguments)
            {
                var aliasDirector = new AliasDirector(this, arguments.TypeArgs);
                aliasDirector.Subscribe(observer);
                return aliasDirector;
            }


            class AliasDirector : ITypeDirector, IDisposable
            {
                readonly ObserverCollection<CodeType> observers = Helper.CreateTypeObserver();
                readonly IDisposable referenceSubscription;

                public AliasDirector(AliasProvider provider, CodeType[] typeArgs)
                {
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