#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby2.Expand;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse.Vanilla.Ide;

static class VanillaCompletion
{
    static readonly IEnumerable<CompletionItem> Keywords = new CompletionItem[] {
        new() {
            Label = "Global",
            Kind = CompletionItemKind.Module,
            InsertText = "Global.",
            SortText = "\"",
            Command = new Command() {
                Title = "Suggest",
                Name = "editor.action.triggerSuggest"
            }
        }
    };

    static readonly IEnumerable<CompletionItem> Values = ElementRoot.Instance.Values.Select(value => GetValueCompletionItem(value));
    static readonly string[] RuleContentNames = new[] { "event", "conditions", "actions" };

    static CompletionItem GetActionCompletionItem(ElementJsonAction action, bool highlight)
    {
        return new()
        {
            Label = highlight ? $"★ {action.Name}" : action.Name,
            SortText = highlight ? $"!{action.Name}" : action.Name,
            FilterText = action.Name,
            InsertText = $"{action.Name}{GetParametersSnippetInsert(action, 1)};$0",
            InsertTextFormat = InsertTextFormat.Snippet,
            Kind = CompletionItemKind.Function,
            Documentation = FunctionSignature(new(), action)
        };
    }

    static CompletionItem GetValueCompletionItem(ElementJsonValue value, bool highlight = false, bool expectingAnotherValue = false)
    {
        bool returnsComparable = value.ReturnType == "vector" || value.ReturnType == "number";
        string separator = expectingAnotherValue ? ", $0" : returnsComparable ? "$0" : "";
        int ci = expectingAnotherValue || returnsComparable ? 1 : 0;
        return new()
        {
            Label = highlight ? $"★ {value.Name}" : value.Name,
            SortText = highlight ? $"!{value.Name}" : value.Name,
            FilterText = value.Name,
            InsertText = $"{value.Name}{GetParametersSnippetInsert(value, ci)}{separator}",
            InsertTextFormat = InsertTextFormat.Snippet,
            Kind = CompletionItemKind.Method,
            Documentation = FunctionSignature(new(), value)
        };
    }

    static CompletionItem[] GetTeamSugarCompletions(bool highlight, bool expectingAnotherValue) => new CompletionItem[] {
        GetTeamSugarCompletion("All Teams", highlight, expectingAnotherValue),
        GetTeamSugarCompletion("Team 1", highlight, expectingAnotherValue),
        GetTeamSugarCompletion("Team 2", highlight, expectingAnotherValue),
    };

    static CompletionItem GetTeamSugarCompletion(string name, bool highlight, bool expectingAnotherValue) => new()
    {
        Label = highlight ? $"★ {name}" : name,
        SortText = highlight ? $"!!{name}" : name,
        FilterText = name,
        InsertText = expectingAnotherValue ? $"{name}, " : name,
        Kind = CompletionItemKind.Constant
    };

    static string GetParametersSnippetInsert(ElementBaseJson function, int ci)
    {
        if (function.Name == "String" || function.Name == "Custom String")
            return $"(\"${ci}\")";
        return function.HasParameters() ? $"(${ci})" : "";
    }

    /// <summary>Creates completion for actions and values.</summary>
    public static ICompletionRange CreateStatementCompletion(DocRange range, BalancedActions balancer) =>
        ICompletionRange.New(range, args =>
        {
            var notable = balancer.GetNotableActionsFromPos(args.Pos);
            return ElementRoot.Instance.Actions
                .Select(action => GetActionCompletionItem(action, notable.Contains(action.Name)))
                .Concat(Values).Concat(Keywords);
        });

    /// <summary>Creates completion for values.</summary>
    public static ICompletionRange CreateValueCompletion(DocRange range) =>
        ICompletionRange.New(range, Values.Concat(Keywords));

    /// <summary>Creates completion for a group of constants (enum).</summary>
    public static ICompletionRange GetConstantsCompletion(DocRange range, ElementEnum constants, DocRange? replaceRange, bool expectingAnotherValue) =>
        ICompletionRange.New(range, constants.Members.Select(member => new CompletionItem()
        {
            Label = member.Name,
            InsertText = expectingAnotherValue ? $"{member.Name}, " : member.Name,
            Kind = CompletionItemKind.Constant,
            TextEdit = replaceRange is null ? null : new(new InsertReplaceEdit()
            {
                NewText = member.Name,
                Replace = replaceRange
            })
        }));

    /// <summary>The signature of a function as markup.</summary>
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

    /// <summary>The signature of a function as a string.</summary>
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

    /// <summary>Creates completion for variables in the scope.</summary>
    public static ICompletionRange GetVariableCompletion(DocRange range, VanillaScope scopedVariables, bool isGlobal, bool expectingAnotherValue) =>
        ICompletionRange.New(range, scopedVariables.GetVariables(isGlobal).Select(v => new CompletionItem()
        {
            Label = v.Name,
            InsertText = expectingAnotherValue ? $"{v.Name}, " : v.Name,
            Kind = CompletionItemKind.Variable,
            Detail = $"({VanillaHelper.GlobalOrPlayerString(isGlobal)} variable) {v.Name}"
        }));

    public static ICompletionRange GetSubroutineCompletion(DocRange range, VanillaScope scopedVariables, bool expectingAnotherValue) =>
        ICompletionRange.New(range, scopedVariables.GetSubroutines().Select(subroutine => new CompletionItem()
        {
            Label = subroutine.Name,
            InsertText = expectingAnotherValue ? $"{subroutine.Name}, " : subroutine.Name,
            Kind = CompletionItemKind.Function,
            Detail = $"(subroutine) {subroutine.Name}"
        }));

    public static ICompletionRange GetValueCompletion(DocRange range, IEnumerable<string> notableValues, bool expectingAnotherValue) =>
        ICompletionRange.New(range,
            ElementRoot.Instance.Values.Select(value =>
                GetValueCompletionItem(value, notableValues.Contains(value.Name), expectingAnotherValue)
            ).Concat(GetTeamSugarCompletions(highlight: notableValues.Contains("Team"), expectingAnotherValue))
            .Concat(Keywords));

    /// <summary>Creates completion for the Event, Team, and Player rule options.</summary>
    public static ICompletionRange CreateEventCompletion(DocRange range, VanillaKeyword[] items) => ICompletionRange.New(
        range,
        getCompletionParams => items.Select(item => new CompletionItem()
        {
            Label = item.EnUs,
            Kind = CompletionItemKind.Constant
        })
    );

    public static ICompletionRange CreateEventDeclarationCompletion(DocRange range) => ICompletionRange.New(
        range,
        RuleContentNames.Select(item => new CompletionItem()
        {
            Label = item,
            Kind = CompletionItemKind.Keyword,
            InsertText = item + " {\n\t$0\n}",
            InsertTextFormat = InsertTextFormat.Snippet
        })
    );

    public static ICompletionRange CreateLobbySettingCompletion(DocRange range, EObject[] objects, IEnumerable<string> alreadyIncluded) =>
        ICompletionRange.New(range, objects.Where(o => !alreadyIncluded.Contains(o.Name)).Select(o => new CompletionItem()
        {
            Label = o.Name,
            Kind = CompletionItemKind.Property,
            InsertText = o.CompletionInsertText(),
            InsertTextFormat = InsertTextFormat.Snippet
        }));

    public static ICompletionRange CreateKeywords(DocRange range, params string[] keywords) =>
        ICompletionRange.New(range, keywords.Select(keyword => new CompletionItem()
        {
            Label = keyword,
            Kind = CompletionItemKind.Constant
        }));

    public static ICompletionRange Clear(DocRange range) => ICompletionRange.New(range, Enumerable.Empty<CompletionItem>());
}