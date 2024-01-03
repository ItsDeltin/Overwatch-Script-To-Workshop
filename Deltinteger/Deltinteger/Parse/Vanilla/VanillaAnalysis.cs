#nullable enable

using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse.Vanilla;

static class VanillaAnalysis
{
    public static void AnalyzeRule(VanillaContext context, VanillaRule rule)
    {
        foreach (var content in rule.Content)
        {
            switch (content.GroupToken.TokenType)
            {
                case TokenType.WorkshopEvent:
                    break;

                case TokenType.WorkshopConditions:
                    break;

                case TokenType.WorkshopActions:
                    break;

                default:
                    context.Error("Unknown rule category may be an issue with translations", content.GroupToken.Range);
                    break;
            }
        }
    }

    public static void AnalyzeContent(VanillaContext context, VanillaRuleContent content)
    {
        foreach (var expression in content.InnerItems)
            AnalyzeExpression(context, expression);
    }

    public static void AnalyzeExpression(VanillaContext context, IVanillaExpressionAnalysis expression)
    {
        switch (expression)
        {
            case NumberExpression number:
                break;

            case VanillaSymbolExpression symbol:
                break;

            case VanillaInvokeExpression invoke:
                break;
        }
    }
}