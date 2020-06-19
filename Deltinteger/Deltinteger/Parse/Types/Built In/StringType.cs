using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class StringType : CodeType, IInitOperations
    {
        public static readonly StringType Instance = new StringType();

        private StringType() : base("String")
        {
            CanBeExtended = false;
            Inherit(ObjectType.Instance, null, null);
        }

        public void InitOperations()
        {
            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, ObjectType.Instance, this, (l, r) => new V_CustomString("{0}{1}", l, r))
            };
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}