using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class StringType : CodeType, IResolveElements
    {
        private readonly ITypeSupplier _typeSupplier;

        public StringType(ITypeSupplier typeSupplier) : base("String")
        {
            _typeSupplier = typeSupplier;
            CanBeExtended = false;
            // Inherit(ObjectType.Instance, null, null);
        }

        public void ResolveElements()
        {
            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, _typeSupplier.Any(), this, (l, r) => new StringElement("{0}{1}", l, r))
            };
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}