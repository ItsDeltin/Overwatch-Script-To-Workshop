using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Generics;
using DS.Analysis.Types.Components;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using DS.Analysis.Core;

namespace DS.Analysis.Structure.TypeAlias
{
    using static Utility2;

    class DeclaredTypeAlias : AbstractDeclaredElement
    {
        readonly IDisposableTypeDirector typeReference;
        readonly ICodeTypeProvider provider;


        public DeclaredTypeAlias(ContextInfo context, ITypeAliasProvider content)
        {
            // Setup
            var setup = content.Setup(context);
            Name = setup.Name;
            typeReference = setup.TypeReference;

            provider = CreateProviderAndDirector(Name, setup.TypeArgs, null /* todo: IGetIdentifier */, arguments =>
            {
                // Create a dependency for the type being referenced.
                arguments.AddDisposable(typeReference.AddDependent(CreateDependent(context.Analysis, () =>
                {
                    // Substitute the type.
                    arguments.SetType(new CodeType(typeReference.Type)
                    {
                        GetIdentifier = GetStructuredIdentifier.Create(
                            Name,
                            arguments.TypeArgs,
                            context.Parent?.GetIdentifier,
                            element => element.TypePartHandler == provider
                        )
                    });
                })));
            });
        }

        public override void AddToScope(IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(ScopedElement.CreateType(Name, provider));
        }

        public override void AddToContent(TypeContentBuilder contentBuilder) => contentBuilder.AddElement(new ProviderTypeElement(provider));


        public override void Dispose()
        {
            typeReference.Dispose();
        }
    }
}