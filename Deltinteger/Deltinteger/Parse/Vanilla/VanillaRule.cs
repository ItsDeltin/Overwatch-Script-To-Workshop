#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse.Vanilla.ToWorkshop;

namespace Deltin.Deltinteger.Parse.Vanilla;

/// <summary>
/// An analyzed vanilla rule.
/// </summary>
record VanillaRuleAnalysis(bool Disabled, string Name, AnalyzedEventOrContent[] Content)
{
    public Result<Rule, string> ToElement(VanillaWorkshopConverter converter)
    {
        var ruleEvent = RuleEvent.OngoingGlobal;
        var team = Team.All;
        var player = PlayerSelector.All;
        var actions = Array.Empty<Element>();
        var conditions = Array.Empty<Condition>();
        string? subroutineName = null;

        foreach (var content in Content)
        {
            if (content.Variant.Get(out var eventData, out var expressionList))
            {
                subroutineName ??= eventData.SubroutineName;

                // Event
                foreach (var (parameter, i) in eventData.Parameters.WithIndex())
                {
                    string enUsName = parameter.Name;
                    // First parameter is the event type.
                    if (i == 0)
                    {
                        ruleEvent = EventInfo.EventFromString(enUsName) ?? RuleEvent.OngoingGlobal;
                    }
                    // Second is the team type.
                    else if (i == 1)
                    {
                        team = EventInfo.TeamFromString(enUsName) ?? Team.All;
                    }
                    // Third is the player type.
                    else if (i == 2)
                    {
                        player = EventInfo.PlayerFromString(enUsName) ?? PlayerSelector.All;
                    }
                }
            }
            else
            {
                switch (expressionList.Type)
                {
                    case VanillaRuleContentType.Conditions:
                        {
                            // Set conditions
                            var conditionResult = expressionList.Expressions.SelectResult(e => e.AsCondition(converter));
                            if (conditionResult.Get(out var enumerateConditions, out var error))
                            {
                                conditions = enumerateConditions.ToArray();
                            }
                            else
                            {
                                return error;
                            }
                            break;
                        }

                    case VanillaRuleContentType.Actions:
                        {
                            // Set actions
                            var actionResults = expressionList.Expressions.SelectResult(e => e.AsAction(converter));
                            if (actionResults.Get(out var enumerateActions, out var error))
                            {
                                actions = enumerateActions.ToArray();
                            }
                            else
                            {
                                return error;
                            }
                            break;
                        }

                    default: break;
                }
            }
        }

        return new Rule(Name, ruleEvent, team, player)
        {
            Disabled = Disabled,
            Actions = actions,
            Conditions = conditions,
            Subroutine = subroutineName
        };
    }
}

/// <summary>
/// Contains either an `AnalyzedRuleEvent` or a `AnalyzedRuleContent`.
/// </summary>
readonly record struct AnalyzedEventOrContent(Variant<AnalyzedRuleEvent, AnalyzedRuleContent> Variant);

/// <summary>
/// Contains a list of event parameters for an analyzed vanilla rule.
/// </summary>
readonly record struct AnalyzedRuleEvent(IReadOnlyList<ElementEnumMember> Parameters, string? SubroutineName);

/// <summary>
/// The analyzed conditions or actions of a vanilla rule.
/// </summary>
readonly record struct AnalyzedRuleContent(VanillaRuleContentType Type, CommentedAnalyzedExpression[] Expressions);

/// <summary>
/// The kind of a rule's content.
/// </summary>
enum VanillaRuleContentType
{
    Unknown,
    Conditions,
    Actions
}

/// <summary>
/// A potentially commented IVanillaNode.
/// </summary>
readonly record struct CommentedAnalyzedExpression(
    string? Comment,
    IVanillaNode Expression
)
{
    public readonly Result<Element, string> AsAction(VanillaWorkshopConverter converter)
    {
        string? comment = Comment;
        return Expression.GetWorkshopElement(converter).MapValue(value =>
        {
            var asElement = (Element)value;
            asElement.Comment = comment;
            return asElement;
        });
    }

    public readonly Result<Condition, string> AsCondition(VanillaWorkshopConverter converter)
    {
        string? comment = Comment;
        return Expression.GetWorkshopElement(converter).MapValue(value => new Condition((Element)value)
        {
            Comment = comment
        });
    }
}