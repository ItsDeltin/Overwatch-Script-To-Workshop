#nullable enable

using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Vanilla.Ide;

namespace Deltin.Deltinteger.Parse.Vanilla;

static class VanillaAnalysis
{
    public static void AnalyzeRule(ScriptFile script, VanillaRule rule, ScopedVanillaVariables scopedVanillaVariables)
    {
        AnalyzeRule(new VanillaContext(script, scopedVanillaVariables), rule);
    }

    public static void AnalyzeRule(VanillaContext context, VanillaRule rule)
    {
        foreach (var content in rule.Content)
        {
            switch (content.GroupToken.TokenType)
            {
                case TokenType.WorkshopEvent:
                    AnalyzeEventContent(context, content);
                    break;

                case TokenType.WorkshopConditions:
                    AnalyzeContent(context, content);
                    break;

                case TokenType.WorkshopActions:
                    AnalyzeContent(context, content);
                    break;

                default:
                    context.Error($"Unknown rule category '{content.GroupToken.Text}'", content.GroupToken.Range);
                    AnalyzeContent(context, content);
                    break;
            }
        }
    }

    public static void AnalyzeContent(VanillaContext context, VanillaRuleContent syntax)
    {
        // action value completion
        context.AddCompletion(VanillaCompletion.CreateActionValueCompletion(syntax.Range));

        foreach (var contentItem in syntax.InnerItems)
            AnalyzeExpression(context, contentItem.Expression);
    }

    public static void AnalyzeEventContent(VanillaContext context, VanillaRuleContent syntax)
    {
        // Analyze expressions.
        for (int i = 0; i < syntax.InnerItems.Length; i++)
        {
            var analysis = AnalyzeExpression(context, syntax.InnerItems[i].Expression);

            if (i == EventTypesOrder.Length)
            {
                context.Error("Too many statements in event category", analysis.DocRange());
            }
            else if (i < EventTypesOrder.Length)
            {
                var itemInformation = analysis.GetSymbolInformation();

                string? eventTypeName = itemInformation.WorkshopConstant?.Enum.Name;
                if (eventTypeName != EventTypesOrder[i])
                {
                    context.Error($"Invalid {EventTypesOrder[i]} option", analysis.DocRange());
                }
            }
        }

        // Add completion between each statement
        DocPos start = syntax.Range.Start;
        for (int i = 0; i < 3; i++)
        {
            Token? nextSemicolon = syntax.InnerItems.ElementAtOrDefault(i).Semicolon;
            DocPos next = nextSemicolon?.Range.Start ?? syntax.Range.End;

            // Todo: show completion per language
            context.AddCompletion(VanillaCompletion.CreateEventCompletion(start + next, i switch
            {
                1 => VanillaInfo.Team,
                2 => VanillaInfo.Player,
                _ => VanillaInfo.Event,
            }));

            if (nextSemicolon is null)
                break;
            start = nextSemicolon.Range.End;
        }
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

    static readonly string[] EventTypesOrder = new[] { "Event", "Team", "Player" };
}