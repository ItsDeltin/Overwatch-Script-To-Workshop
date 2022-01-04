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

            aliasProvider = new AliasProvider(Name, setup.TypeArgs, typeReference, context.Parent?.GetIdentifier);
        }

        public override void AddToScope(IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(ScopedElement.Create(Name, aliasProvider, null, new ProviderPartHandler(aliasProvider)));
        }

        public override void AddToContent(TypeContentBuilder contentBuilder) => contentBuilder.AddElement(new ProviderTypeElement(aliasProvider));


        public override void Dispose()
        {
            typeReference.Dispose();
        }


        class AliasProvider : CodeTypeProvider
        {
            readonly ITypeDirector aliasing;
            readonly IGetIdentifier parent;


            public AliasProvider(string name, TypeArgCollection typeArgs, ITypeDirector aliasing, IGetIdentifier parent) : base(name, typeArgs)
            {
                this.aliasing = aliasing;
                this.parent = parent;
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
                        // Substitute the type.
                        CodeType substitution = new CodeType(type)
                        {
                            GetIdentifier = GetStructuredIdentifier.Create(
                                provider.Name,
                                typeArgs,
                                provider.parent,
                                element => element.Provider == provider
                            )
                        };
                        observers.Set(substitution);
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