using System;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class AnonymousType : CodeType
    {
        private readonly int _index;

        public AnonymousType(string name, int index) : base(name)
        {
            _index = index;
        }

        public override void GetRealType(ParseInfo parseInfo, Action<CodeType> callback) =>
            parseInfo.SourceExpression.OnResolve(expr => callback(expr.Type().Generics[_index]));

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.TypeParameter
        };
    }
}