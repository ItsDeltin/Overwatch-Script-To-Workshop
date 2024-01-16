#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Vanilla.Ide;
using Deltin.WorkshopString;

namespace Deltin.Deltinteger.Parse.Vanilla;

static class VanillaAnalysis
{
    public static IAnalyzedVanillaCollection AnalyzeCollection(ScriptFile script, VanillaVariableCollection syntax)
    {
        return syntax.IsSubroutineCollection() ?
            VanillaSubroutineAnalysis.Analyze(script, syntax) :
            VanillaVariableAnalysis.Analyze(script, syntax);
    }

    public static VanillaRuleAnalysis AnalyzeRule(ScriptFile script, VanillaRule rule, VanillaScope scopedVanillaVariables)
    {
        return AnalyzeRule(new VanillaContext(script, scopedVanillaVariables), rule);
    }

    public static VanillaRuleAnalysis AnalyzeRule(VanillaContext context, VanillaRule rule)
    {
        string name = WorkshopStringUtility.WorkshopStringFromRawText(rule.Name?.Text) ?? string.Empty;
        bool disabled = rule.Disabled;
        var content = new List<AnalyzedEventOrContent>();

        foreach (var contentGroup in rule.Content)
        {
            switch (contentGroup.GroupToken.TokenType)
            {
                // Rule events
                case TokenType.WorkshopEvent:
                    content.Add(new(AnalyzeEventContent(context, contentGroup)));
                    break;

                // Rule conditions
                case TokenType.WorkshopConditions:
                    content.Add(new(new AnalyzedRuleContent(
                        VanillaRuleContentType.Conditions,
                        AnalyzeContent(context, contentGroup))));
                    break;

                // Rule actions
                case TokenType.WorkshopActions:
                    content.Add(new(new AnalyzedRuleContent(
                        VanillaRuleContentType.Actions,
                        AnalyzeContent(context, contentGroup))));
                    break;

                // Unknown category
                default:
                    context.Error($"Unknown rule category '{contentGroup.GroupToken.Text}'", contentGroup.GroupToken.Range);
                    content.Add(new(new AnalyzedRuleContent(
                        VanillaRuleContentType.Unknown,
                        AnalyzeContent(context, contentGroup))));
                    break;
            }
        }
        return new VanillaRuleAnalysis(disabled, name, content.ToArray());
    }

    public static CommentedAnalyzedExpression[] AnalyzeContent(VanillaContext context, VanillaRuleContent syntax)
    {
        // action value completion
        context.AddCompletion(VanillaCompletion.CreateActionValueCompletion(syntax.Range));

        var analyzedExpressions = new List<CommentedAnalyzedExpression>();
        foreach (var contentItem in syntax.InnerItems)
        {
            var comment = WorkshopStringUtility.WorkshopStringFromRawText(contentItem.Comment?.Text);
            var node = AnalyzeExpression(context, contentItem.Expression);

            // Make sure it is the right type.

            analyzedExpressions.Add(new(comment, node));
        }

        return analyzedExpressions.ToArray();
    }

    public static AnalyzedRuleEvent AnalyzeEventContent(VanillaContext context, VanillaRuleContent syntax)
    {
        var parameters = new List<ElementEnumMember>();
        bool isSubroutine = false;
        string? subroutineName = null;

        // Analyze expressions.
        for (int i = 0; i < syntax.InnerItems.Length; i++)
        {
            var analysis = AnalyzeExpression(
                context.SetActiveParameterData(new(
                    ExpectingSubroutine: isSubroutine,
                    // This will allow symbol analysis to select the right keyword.
                    ExpectingType: isSubroutine || i >= EventTypesOrder.Length
                        ? null
                        : context.VanillaTypeFromJsonName(EventTypesOrder[i])
                )),
                syntax.InnerItems[i].Expression);
            var itemInformation = analysis.GetSymbolInformation();

            if (i == (isSubroutine ? 2 : EventTypesOrder.Length))
            {
                context.Error("Too many statements in event category", analysis.DocRange());
            }
            else if (isSubroutine)
            {
                subroutineName = itemInformation.SymbolName;
            }
            else if (i < EventTypesOrder.Length && !isSubroutine)
            {
                var constant = itemInformation.WorkshopConstant;
                // Ensure the constant is the right type.
                string? eventTypeName = constant?.Enum.Name;
                if (eventTypeName != EventTypesOrder[i])
                {
                    context.Error($"Invalid {EventTypesOrder[i]} option", analysis.DocRange());
                }
                else
                {
                    parameters.Add(constant!);
                    isSubroutine |= i == 0 && constant!.Name == "Subroutine";
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
            if (i == 0 || !isSubroutine)
            {
                context.AddCompletion(VanillaCompletion.CreateEventCompletion(start + next, i switch
                {
                    1 => VanillaInfo.Team,
                    2 => VanillaInfo.Player,
                    _ => VanillaInfo.Event,
                }));
            }
            else if (i == 1)
            {
                context.AddCompletion(VanillaCompletion.GetSubroutineCompletion(start + next, context.ScopedVariables));
            }

            if (nextSemicolon is null)
                break;
            start = nextSemicolon.Range.End;
        }

        return new(parameters, subroutineName);
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

            // Assignment
            case VanillaAssignmentExpression assignment:
                return VanillaExpressions.Assignment(context, assignment);

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