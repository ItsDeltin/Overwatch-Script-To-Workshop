using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using CompletionItemTag = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemTag;
using InsertTextFormat = OmniSharp.Extensions.LanguageServer.Protocol.Models.InsertTextFormat;

namespace Deltin.Deltinteger.LanguageServer
{
    public static class Snippet
    {
        // For snippet.
        public static readonly CompletionItem For = MakeSnippet(
            label: "for",
            detail: "for loop (ostw)",
            documentation: new MarkupBuilder().StartCodeLine().Add("for (define i = 0; i < length; i++)").NewLine().Add("{").NewLine().NewLine().Add("}").EndCodeLine(),
            insert: "for (define ${1:i} = 0; $1 < ${2:length}; $1++)\n{\n    $0\n}"
        );

        // Reverse for snippet.
        public static readonly CompletionItem Forr = MakeSnippet(
            label: "forr",
            detail: "Reverse for loop (ostw)",
            documentation: new MarkupBuilder().StartCodeLine().Add("for (define i = length - 1; i >= 0; i--)").NewLine().Add("{").NewLine().NewLine().Add("}").EndCodeLine(),
            insert: "for (define ${1:i} = ${2:length} - 1; $1 >= 0; $1--)\n{\n    $0\n}"
        );

        // Creates the CompletionItem for a snippet.
        static CompletionItem MakeSnippet(string label, string detail, MarkupBuilder documentation, string insert) => new CompletionItem() {
            Kind = CompletionItemKind.Snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Label = label,
            InsertText = insert,
            Detail = detail,
            Documentation = documentation.ToMarkup()
        };
    }
}