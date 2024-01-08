#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse.Vanilla;

interface IVanillaNode
{
    public DocRange DocRange();
    public VanillaType Type();

    public static IVanillaNode New(IVanillaExpression node)
    {
        return new VanillaExpression(node.Range);
    }

    record class VanillaExpression(DocRange Range) : IVanillaNode
    {
        public DocRange DocRange() => Range;
        public VanillaType Type()
        {
            throw new NotImplementedException();
        }
    }
}

static class VanillaExpressions
{
    public static IVanillaNode Symbol(VanillaContext context, VanillaSymbolExpression syntax)
    {
        void UnknownSymbol()
        {
            context.Warning("Unknown workshop symbol", syntax.Range);
        }

        var parameterData = context.GetActiveParameterData();

        if (syntax.Token is WorkshopToken workshopToken)
        {
            // Narrow down a symbol from the potential items.
            var items = FilterItemsFromContext(context, workshopToken.WorkshopItems);

            if (items.Length == 0)
            {
                UnknownSymbol();
            }
            else
            {
                if (items.Length > 1)
                {
                    context.Info("This symbol is ambiguous, there may translation problems", syntax.Range);
                }

                var selected = items[0];
            }
        }
        else
        {
            UnknownSymbol();
        }

        return IVanillaNode.New(syntax);
    }

    static WorkshopItem[] FilterItemsFromContext(VanillaContext context, IEnumerable<LanguageLinkedWorkshopItem> items)
    {
        // Filter by parameter count
        int? invokeParameterCount = context.InvokeParameterCount();
        var filterParameters = items.Where(item => item.Item switch
        {
            WorkshopItem.ActionValue actionValue => invokeParameterCount.HasValue &&
                invokeParameterCount.Value == (actionValue.Value.Parameters?.Length ?? 0),
            WorkshopItem.Enumerator enumerator => !invokeParameterCount.HasValue,
            _ => false
        });
        if (filterParameters.Any())
            items = filterParameters;

        // Filter by language
        var likelyLanguages = context.LikelyLanguages();
        if (likelyLanguages is not null)
        {
            var filterLanguages = items.Where(item => likelyLanguages.Contains(item.Language));
            if (filterLanguages.Any())
                items = filterLanguages;
        }

        // Get unique items.
        return items.Select(langItem => langItem.Item).Distinct().ToArray();
    }

    public static IVanillaNode Invoke(VanillaContext context, VanillaInvokeExpression syntax)
    {
        // Analyse invoked value
        VanillaAnalysis.AnalyzeExpression(context, syntax.Invoking);

        return IVanillaNode.New(syntax);
    }

    public static IVanillaNode Binary(VanillaContext context, VanillaBinaryOperatorExpression syntax)
    {
        var left = VanillaAnalysis.AnalyzeExpression(context, syntax.Left);
        var right = VanillaAnalysis.AnalyzeExpression(context, syntax.Right);

        return IVanillaNode.New(syntax);
    }
}