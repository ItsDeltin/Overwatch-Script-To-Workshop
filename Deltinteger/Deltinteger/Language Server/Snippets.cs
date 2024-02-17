using System.Collections.Generic;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using CompletionItemTag = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemTag;
using InsertTextFormat = OmniSharp.Extensions.LanguageServer.Protocol.Models.InsertTextFormat;

#nullable enable

namespace Deltin.Deltinteger.LanguageServer
{
    public static class Snippet
    {
        // Rule snippet.
        public static readonly CompletionItem Rule = MakeSnippet(
            label: "rule",
            detail: "rule (ostw)",
            documentation: new MarkupBuilder().StartCodeLine().Add("rule: 'My Rule'").NewLine().Add("{").NewLine().NewLine().Add("}").EndCodeLine(),
            insert: "rule: '${1:My Rule}'\n{\n    $0\n}"
        );

        public static readonly CompletionItem PlayerRule = MakeSnippet(
            label: "rule (player)",
            detail: "player rule (ostw)",
            insert: """
            rule: '${1:My Player Rule}'
            Event.OngoingPlayer
            Team.All
            Player.All
            {
                $0
            }
            """
        );

        // Struct
        public static readonly CompletionItem Struct = MakeSnippet(
            label: "struct",
            detail: "struct (ostw)",
            insert: """
            struct ${1:NewStruct} {
                public ${3:Any} ${2:Variable};

                public static $1 New($3 value): { $2: value };
            }
            """
        );

        // Single Struct
        public static readonly CompletionItem SingleStruct = MakeSnippet(
            label: "single struct",
            detail: "single struct (ostw)",
            insert: """
            single struct ${1:NewStruct} {
                public ${3:Any} ${2:Variable};

                public static $1 New($3 value): single { $2: value };
            }
            """
        );

        // class
        public static readonly CompletionItem Class = MakeSnippet(
            label: "class",
            detail: "class (ostw)",
            insert: """
            class ${1:NewClass} {
                public constructor() {
                    $0
                }
            }
            """
        );

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

        // Auto for.
        public static readonly CompletionItem AutoFor = MakeSnippet(
            label: "fora",
            detail: "Auto for loop (ostw)",
            documentation: new MarkupBuilder().StartCodeLine().Add("for (define i = 0; length; 1)").NewLine().Add("{").NewLine().NewLine().Add("}").EndCodeLine(),
            insert: "for (define ${1:i} = 0; ${2:length}; 1)\n{\n    $0\n}"
        );

        // Auto for.
        public static readonly CompletionItem AutoForr = MakeSnippet(
            label: "forra",
            detail: "Reverse auto for loop (ostw)",
            documentation: new MarkupBuilder().StartCodeLine().Add("for (define i = length - 1; -1; -1)").NewLine().Add("{").NewLine().NewLine().Add("}").EndCodeLine(),
            insert: "for (define ${1:i} = ${2:length} - 1; -1; -1)\n{\n    $0\n}"
        );

        // Vanilla
        public static readonly CompletionItem EnUsVanillaRule = MakeSnippet(
            label: "workshop rule",
            detail: "workshop rule (ow)",
            documentation: new MarkupBuilder(),
            insert: """
            rule("$1") {
                event {
                    $0
                }
            }
            """
        );

        public static readonly CompletionItem EnUsSettings = MakeSnippet(
            label: "workshop settings",
            detail: "workshop settings (ow)",
            documentation: new MarkupBuilder(),
            insert: """
            settings {
                $0
            }
            """
        );

        public static readonly CompletionItem EnUsVariables = MakeSnippet(
            label: "workshop variables",
            detail: "workshop variables (ow)",
            documentation: new MarkupBuilder(),
            insert: """
            variables {
                ${1|global,player|}:
                    ${2:0}: ${3:myVariable}
            }$0
            """
        );

        public static readonly CompletionItem EnUsSubroutines = MakeSnippet(
            label: "workshop subroutines",
            detail: "workshop subroutines (ow)",
            documentation: new MarkupBuilder(),
            insert: """
            subroutines {
                ${1:0}: ${2:mySubroutine}
            }$0
            """
        );

        public static IEnumerable<CompletionItem> Snippets = new[] {
            Rule,
            PlayerRule,
            Struct,
            SingleStruct,
            Class,
            EnUsVanillaRule,
            EnUsSettings,
            EnUsVariables,
            EnUsSubroutines
        };

        // Creates the CompletionItem for a snippet.
        static CompletionItem MakeSnippet(string label, string detail, string insert, MarkupBuilder? documentation = null) => new CompletionItem()
        {
            Kind = CompletionItemKind.Snippet,
            InsertTextFormat = InsertTextFormat.Snippet,
            Label = label,
            InsertText = insert,
            Detail = detail,
            Documentation = documentation?.ToMarkup()
        };
    }
}