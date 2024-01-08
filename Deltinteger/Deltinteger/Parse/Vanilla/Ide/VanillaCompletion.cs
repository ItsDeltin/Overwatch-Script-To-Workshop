#nullable enable
using System.Linq;
using Deltin.Deltinteger.Compiler;
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
            builder.NewLine().Add("Returns: ").Code(value.ReturnType);
        }

        return builder;
    }
}