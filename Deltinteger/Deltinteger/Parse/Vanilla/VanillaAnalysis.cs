#nullable enable

using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Vanilla.Ide;

namespace Deltin.Deltinteger.Parse.Vanilla;

static class VanillaAnalysis
{
    public static void AnalyzeVariables(ScriptFile script, VanillaVariableCollection variables)
    {

    }

    public static void AnalyzeRule(ScriptFile script, VanillaRule rule)
    {
        AnalyzeRule(new VanillaContext(script), rule);
    }

    public static void AnalyzeRule(VanillaContext context, VanillaRule rule)
    {
        foreach (var content in rule.Content)
        {
            switch (content.GroupToken.TokenType)
            {
                case TokenType.WorkshopEvent:
                    AnalyzeContent(context, content);
                    break;

                case TokenType.WorkshopConditions:
                    AnalyzeContent(context, content);
                    break;

                case TokenType.WorkshopActions:
                    AnalyzeContent(context, content);
                    break;

                default:
                    context.Error($"Unknown rule category '{content.GroupToken.Text}'", content.GroupToken.Range);
                    break;
            }
        }
    }

    public static void AnalyzeContent(VanillaContext context, VanillaRuleContent contentSyntax)
    {
        context.AddCompletion(VanillaCompletion.CreateCompletion(contentSyntax.Range));

        foreach (var expression in contentSyntax.InnerItems)
            AnalyzeExpression(context, expression);
    }

    public static IVanillaNode AnalyzeExpression(VanillaContext context, IVanillaExpression expression)
    {
        switch (expression)
        {
            // Grouped expression
            case ParenthesizedVanillaExpression grouped:
                return VanillaExpressions.Grouped(context, grouped);

            // Number expression
            case NumberExpression number:
                return VanillaExpressions.Number(context, number);

            // String expression
            case VanillaStringExpression str:
                return VanillaExpressions.String(context, str);

            // Workshop symbol or identifier
            case VanillaSymbolExpression symbol:
                return VanillaExpressions.Symbol(context, symbol);

            // Invoke
            case VanillaInvokeExpression invoke:
                return VanillaExpressions.Invoke(context, invoke);

            // Binary operator
            case VanillaBinaryOperatorExpression binary:
                return VanillaExpressions.Binary(context, binary);

            // The parser will add an error if the value is missing, nothing needs to happen here.
            case MissingVanillaExpression:
                break;

            // Bug: There is a type not handled here.
            default:
                context.Error($"Internal error: unknown expression type '{expression.GetType()}'", expression.Range);
                break;
        }
        // Missing
        return IVanillaNode.New(expression, () => "Missing nodes cannot be converted to the workshop");
    }
}