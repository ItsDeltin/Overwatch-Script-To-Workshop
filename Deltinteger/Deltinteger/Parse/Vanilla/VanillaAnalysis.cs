#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using EventInfo = Deltin.Deltinteger.Decompiler.TextToElement.EventInfo;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Vanilla.Ide;
using Deltin.WorkshopString;
using SymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;

namespace Deltin.Deltinteger.Parse.Vanilla;

static class VanillaAnalysis
{
    public static IAnalyzedVanillaCollection AnalyzeCollection(ScriptFile script, VanillaVariableCollection syntax)
    {
        return syntax.IsSubroutineCollection() ?
            VanillaSubroutineAnalysis.Analyze(script, syntax) :
            VanillaVariableAnalysis.Analyze(script, syntax);
    }

    public static VanillaRuleAnalysis AnalyzeRule(ScriptFile script, VanillaRule rule, VanillaScope scopedVanillaVariables, IdeItems ideItems)
    {
        return AnalyzeRule(new VanillaContext(script, scopedVanillaVariables, ideItems), rule);
    }

    public static VanillaRuleAnalysis AnalyzeRule(VanillaContext context, VanillaRule rule)
    {
        string name = WorkshopStringUtility.WorkshopStringFromRawText(rule.Name?.Text) ?? string.Empty;
        bool disabled = rule.Disabled;
        var content = new List<AnalyzedEventOrContent>();
        RuleEvent? eventType = null;
        bool gotEvent = false, gotConditions = false, gotActions = false;

        foreach (var contentGroup in rule.Content)
        {
            switch (contentGroup.GroupToken.TokenType)
            {
                // Rule events
                case TokenType.WorkshopEvent:
                    if (gotEvent)
                        context.Error("Duplicate 'event' definition", contentGroup.GroupToken);

                    gotEvent = true;
                    var eventInfo = AnalyzeEventContent(context, contentGroup);
                    content.Add(new(eventInfo));
                    eventType = eventInfo.GetEventType() ?? eventType;
                    break;

                // Rule conditions
                case TokenType.WorkshopConditions:
                    if (gotConditions)
                        context.Error("Duplicate 'conditions' definition", contentGroup.GroupToken);

                    gotConditions = true;
                    content.Add(new(new AnalyzedRuleContent(
                        VanillaRuleContentType.Conditions,
                        AnalyzeContent(context.SetEventType(eventType), contentGroup, RuleContentType.Conditions))));
                    break;

                // Rule actions
                case TokenType.WorkshopActions:
                    if (gotActions)
                        context.Error("Duplicate 'actions' definition", contentGroup.GroupToken);

                    gotActions = true;
                    content.Add(new(new AnalyzedRuleContent(
                        VanillaRuleContentType.Actions,
                        AnalyzeContent(context.SetEventType(eventType), contentGroup, RuleContentType.Actions))));
                    break;

                // Unknown category
                default:
                    context.Error($"Unknown rule category '{contentGroup.GroupToken.Text}'", contentGroup.GroupToken.Range);
                    content.Add(new(new AnalyzedRuleContent(
                        VanillaRuleContentType.Unknown,
                        AnalyzeContent(context.SetEventType(eventType), contentGroup, RuleContentType.Unknown))));
                    break;
            }
        }

        // Missing event error
        if (!gotEvent && rule.Keyword is not null)
            context.Error("Missing 'event' definition", rule.Keyword);

        // Add completion for event, conditions, and actions.
        if (rule.Begin && rule.End)
        {
            var contentRange = rule.Begin!.Range.End + rule.End!.Range.Start;

            List<string> missingCategories = new(3);
            if (!gotEvent) missingCategories.Add("event");
            if (!gotConditions) missingCategories.Add("conditions");
            if (!gotActions) missingCategories.Add("actions");

            context.AddCompletion(VanillaCompletion.CreateEventDeclarationCompletion(contentRange, missingCategories));
        }

        // Empty names are not allowed in LSP
        if (!string.IsNullOrEmpty(name))
        {
            context.AddDocumentSymbol(new DocumentSymbolNode(
                name,
                eventType == RuleEvent.Subroutine ? SymbolKind.Function : SymbolKind.Event,
                rule.Range,
                rule.Name ?? rule.Keyword ?? rule.Disabled,
                eventType is null ? null : EventInfo.EventToString(eventType.Value)));
        }

        return new VanillaRuleAnalysis(disabled, name, content.ToArray());
    }

    enum RuleContentType
    {
        Unknown,
        Conditions,
        Actions
    }

    static CommentedAnalyzedExpression[] AnalyzeContent(VanillaContext context, VanillaRuleContent syntax, RuleContentType contentType)
    {
        var balancer = new BalancedActions();
        context = context.AddActionBalancer(balancer);

        var analyzedExpressions = new List<CommentedAnalyzedExpression>();
        foreach (var statement in syntax.InnerItems)
        {
            // Update balancer position to the next semicolon.
            if (statement.Semicolon is not null)
            {
                balancer.SetCurrentPosition(statement.Semicolon.Range.Start);
            }
            var comment = WorkshopStringUtility.WorkshopStringFromRawText(statement.Comment?.Text);
            var node = AnalyzeExpression(context, statement.Expression);
            analyzedExpressions.Add(new(comment, statement.Disabled, node));

            // Make sure it is the right type.
            var isAction = node.GetSymbolInformation().IsAction;
            var doNotError = node.GetSymbolInformation().DoNotError;

            if (!doNotError)
            {
                // Should be value, got action.
                if (contentType == RuleContentType.Conditions && isAction)
                    context.Error("Expected value, got an action", statement.Expression.Range);

                // Should be action, got value.
                else if (contentType == RuleContentType.Actions && !isAction)
                    context.Error("Expected action, got a value", statement.Expression.Range);
            }
        }

        // action value completion
        context.AddCompletion(VanillaCompletion.CreateStatementCompletion(syntax.Range, balancer, contentType != RuleContentType.Conditions));

        return analyzedExpressions.ToArray();
    }

    public static AnalyzedRuleEvent AnalyzeEventContent(VanillaContext context, VanillaRuleContent syntax)
    {
        var parameters = new List<ElementEnumMember>();
        TopKind topKind = TopKind.Unknown;
        string? subroutineName = null;

        // Analyze expressions.
        for (int i = 0; i < syntax.InnerItems.Length; i++)
        {
            var analysis = AnalyzeExpression(
                context.SetActiveParameterData(new(
                    ExpectingSubroutine: IsSubroutine(topKind),
                    // This will allow symbol analysis to select the right keyword.
                    ExpectingType: IsSubroutine(topKind) || i >= EventTypesOrder.Length
                        ? null
                        : context.VanillaTypeFromJsonName(EventTypesOrder[i])
                )),
                syntax.InnerItems[i].Expression);
            var itemInformation = analysis.GetSymbolInformation();

            // Three options for events, two options for subroutines.
            if (i == GetExpectedItemCount(topKind))
            {
                context.Error("Too many statements in event category", analysis.DocRange());
            }
            else if (IsSubroutine(topKind))
            {
                subroutineName = itemInformation.SymbolName;
            }
            else if (i < EventTypesOrder.Length && !IsSubroutine(topKind))
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

                    if (i == 0)
                    {
                        topKind = constant!.Name switch
                        {
                            "Subroutine" => TopKind.Subroutine,
                            "Ongoing - Global" => TopKind.Global,
                            _ => TopKind.PlayerLike
                        };
                    }
                }
            }
        }

        // Error if there are not enough options.
        if (syntax.InnerItems.Length < GetExpectedItemCount(topKind))
        {
            context.Error($"Expected {GetExpectedItemCount(topKind)} rule parameters", syntax.GroupToken);
        }

        // Add completion between each statement
        DocPos start = syntax.Range.Start;
        for (int i = 0; i < 3; i++)
        {
            Token? nextSemicolon = syntax.InnerItems.ElementAtOrDefault(i).Semicolon;
            DocPos next = nextSemicolon?.Range.Start ?? syntax.Range.End;

            // Todo: show completion per language
            if (i == 0 || !IsSubroutine(topKind))
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
                context.AddCompletion(VanillaCompletion.GetSubroutineCompletion(start + next, context.ScopedVariables, false));
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

            // Indexer
            case VanillaIndexerExpression indexer:
                return VanillaExpressions.Indexer(context, indexer);

            // Binary operator
            case VanillaBinaryOperatorExpression binary:
                return VanillaExpressions.Binary(context, binary);

            // Not
            case VanillaNotExpression not:
                return VanillaExpressions.Not(context, not);

            // Ternary
            case VanillaTernaryExpression ternary:
                return VanillaExpressions.Ternary(context, ternary);

            // Assignment
            case VanillaAssignmentExpression assignment:
                return VanillaExpressions.Assignment(context, assignment);

            // "All Teams", "Team 1", and "Team 2"
            case VanillaTeamSugarExpression teamSugar:
                return VanillaExpressions.TeamSugar(context, teamSugar);

            // The parser will add an error if the value is missing, nothing needs to happen here.
            case MissingVanillaExpression:
                break;

            // Bug: There is a type not handled here.
            default:
                context.Error($"Internal error: unknown expression type '{expression.GetType()}'", expression.Range);
                break;
        }
        // Missing
        return IVanillaNode.New(expression, _ => "Missing nodes cannot be converted to the workshop");
    }

    static readonly string[] EventTypesOrder = new[] { "Event", "Team", "Player" };

    static bool IsSubroutine(TopKind topKind) => topKind == TopKind.Subroutine;

    static int GetExpectedItemCount(TopKind topKind) => topKind switch
    {
        TopKind.Subroutine => 2,
        TopKind.Global => 1,
        _ => 3
    };

    enum TopKind
    {
        Unknown,
        Subroutine,
        Global,
        PlayerLike
    }
}