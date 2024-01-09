#nullable enable
using System;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse.Vanilla.Ide;

static class VanillaCompletion
{
    static readonly CompletionItem[] VanillaCompletionItems = GetItems();

    public static ICompletionRange CreateCompletion(DocRange range) =>
        ICompletionRange.New(range, CompletionRangeKind.Catch, param => VanillaCompletionItems);

    static CompletionItem[] GetItems()
    {
        // Actions and values
        return ElementRoot.Instance.Actions.Select(action => new CompletionItem()
        {
            Label = action.Name,
            Kind = CompletionItemKind.Function,
            Documentation = FunctionSignature(new(), action)
        }).Concat(ElementRoot.Instance.Values.Select(value => new CompletionItem()
        {
            Label = value.Name,
            Kind = CompletionItemKind.Method,
            Documentation = FunctionSignature(new(), value)
        })).ToArray();
    }

    public static CompletionItem[] GetConstantsCompletion(ElementEnum constants, DocRange replaceRange)
    {
        return constants.Members.Select(member => new CompletionItem()
        {
            Label = member.Name,
            Kind = CompletionItemKind.Constant,
            TextEdit = new(new InsertReplaceEdit()
            {
                NewText = member.Name,
                Replace = replaceRange
            })
        }).ToArray();
    }

    public static MarkupBuilder FunctionSignature(MarkupBuilder builder, ElementBaseJson workshopFunction)
    {
        builder.StartCodeLine("ow").Add($"{workshopFunction.Name}");

        if (workshopFunction.Parameters is not null && workshopFunction.Parameters.Length > 0)
        {
            builder.Add("(");
            builder.Add(string.Join(", ", workshopFunction.Parameters.Select(p => p.Name)));
            builder.Add(")");
        }

        builder.EndCodeLine().NewLine().Add(workshopFunction.Documentation);

        if (workshopFunction is ElementJsonValue value)
        {
            builder.NewLine().NewLine().Add("Returns: ").Code(value.ReturnType);
        }

        return builder;
    }

    public static string FunctionSignatureString(ElementBaseJson workshopFunction)
    {
        string result = workshopFunction.Name;

        if (workshopFunction.Parameters is not null && workshopFunction.Parameters.Length > 0)
        {
            result += $"({string.Join(", ", workshopFunction.Parameters.Select(p => p.Name + "‎"))})";
        }
        return result;
    }

    public static int? GetActiveParameter(VanillaInvokeExpression expression, DocPos caretPos)
    {
        int selected = 0;
        for (int i = 1; i < expression.Arguments.Count; i++)
        {
            if (caretPos >= expression.Arguments[i].PreceedingComma.Range.End)
            {
                selected = i;
            }
            else break;
        }
        return selected;
    }

    public static SignatureInformation GetFunctionSignatureInformation(ElementBaseJson element, int? activeParameter)
    {
        var args = element.Parameters ?? Array.Empty<ElementParameter>();
        return new SignatureInformation()
        {
            Label = FunctionSignatureString(element!),
            Parameters = args.Select(p => new ParameterInformation()
            {
                Label = p.Name + "‎",
                Documentation = p.Documentation
            }).ToArray(),
            ActiveParameter = activeParameter
        };
    }
}