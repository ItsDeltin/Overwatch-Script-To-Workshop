using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Generics;
using DS.Analysis.Types.Components;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using DS.Analysis.Core;

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
            scopeAppender.AddScopedElement(ScopedElement.CreateType(Name, aliasProvider));
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


            class AliasDirector : IDisposableTypeDirector
            {
                public CodeType Type { get; private set; }

                readonly DependencyHandler dependencyHandler;

                public AliasDirector(IMaster master, AliasProvider provider, CodeType[] typeArgs)
                {
                    // Watch the type being aliased
                    dependencyHandler = new DependencyHandler(master, updateHelper =>
                    {
                        // Substitute the type
                        Type = new CodeType(provider.aliasing.Type)
                        {
                            GetIdentifier = GetStructuredIdentifier.Create(
                                provider.Name,
                                typeArgs,
                                provider.parent,
                                element => element.TypePartHandler == provider
                            )
                        };

                        dependencyHandler.MakeDependentsStale();
                    });
                    dependencyHandler.DependOn(provider.aliasing);
                }

                public IDisposable AddDependent(IDependent dependent) => dependencyHandler.AddDependent(dependent);

                public void Dispose() => dependencyHandler.Dispose();
            }
        }
    }
}