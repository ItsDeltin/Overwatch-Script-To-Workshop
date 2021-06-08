using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class BooleanType : CodeType
    {
        private readonly ITypeSupplier _supplier;

        public BooleanType(ITypeSupplier supplier) : base("Boolean")
        {
            CanBeExtended = false;
            _supplier = supplier;

            Operations.AddTypeOperation(new TypeOperation[] {
                new TypeOperation(TypeOperator.And, this, this),
                new TypeOperation(TypeOperator.Or, this, this),
            });
        }

        protected override bool DoesImplement(CodeType type) => base.DoesImplement(type) || type.Implements(_supplier.Number());

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}
