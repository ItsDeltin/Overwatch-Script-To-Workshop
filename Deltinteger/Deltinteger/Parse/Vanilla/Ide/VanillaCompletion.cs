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
    static readonly IEnumerable<CompletionItem> Actions = ElementRoot.Instance.Actions.Select(action => new CompletionItem()
    {
        Label = action.Name,
        InsertText = $"{action.Name}{GetParametersSnippetInsert(action)};$0",
        InsertTextFormat = InsertTextFormat.Snippet,
        Kind = CompletionItemKind.Function,
        Documentation = FunctionSignature(new(), action)
    });

    static readonly IEnumerable<CompletionItem> Values = ElementRoot.Instance.Values.Select(value => GetValueCompletionItem(value));

    static CompletionItem GetValueCompletionItem(ElementJsonValue value, bool highlight = false) => new()
    {
        Label = highlight ? $"★ {value.Name}" : value.Name,
        SortText = highlight ? $"!{value.Name}" : value.Name,
        InsertText = $"{value.Name}{GetParametersSnippetInsert(value)}$0",
        InsertTextFormat = InsertTextFormat.Snippet,
        Kind = CompletionItemKind.Method,
        Documentation = FunctionSignature(new(), value)
    };

    static string GetParametersSnippetInsert(ElementBaseJson function)
    {
        if (function.Name == "String" || function.Name == "Custom String")
            return "(\"$1\")";
        return function.HasParameters() ? "($1)" : "";
    }

    /// <summary>Creates completion for actions and values.</summary>
    public static ICompletionRange CreateActionValueCompletion(DocRange range) =>
        ICompletionRange.New(range, CompletionRangeKind.Catch, param => Actions.Concat(Values));

    /// <summary>Creates completion for values.</summary>
    public static ICompletionRange CreateValueCompletion(DocRange range) =>
        ICompletionRange.New(range, CompletionRangeKind.Catch, param => Values);

    /// <summary>Creates completion for a group of constants (enum).</summary>
    public static CompletionItem[] GetConstantsCompletion(ElementEnum constants, DocRange? replaceRange)
    {
        return constants.Members.Select(member => new CompletionItem()
        {
            Label = member.Name,
            Kind = CompletionItemKind.Constant,
            TextEdit = replaceRange is null ? null : new(new InsertReplaceEdit()
            {
                NewText = member.Name,
                Replace = replaceRange
            })
        }).ToArray();
    }

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
    public static CompletionItem[] GetVariableCompletion(VanillaScope scopedVariables, bool isGlobal) =>
        scopedVariables.GetVariables(isGlobal).Select(v => new CompletionItem()
        {
            Label = v.Name,
            Kind = CompletionItemKind.Variable,
            Detail = $"({VanillaHelper.GlobalOrPlayerString(isGlobal)} variable) {v.Name}"
        }).ToArray();

    public static ICompletionRange GetSubroutineCompletion(DocRange range, VanillaScope scopedVariables) => ICompletionRange.New(range, args =>
        scopedVariables.GetSubroutines().Select(subroutine => new CompletionItem()
        {
            Label = subroutine.Name,
            Kind = CompletionItemKind.Function,
            Detail = $"(subroutine) {subroutine.Name}"
        }).ToArray());

    public static ICompletionRange GetValueCompletion(DocRange range, IEnumerable<string> notableValues) => ICompletionRange.New(range,
        ElementRoot.Instance.Values.Select(value => GetValueCompletionItem(value, notableValues.Contains(value.Name))));

    /// <summary>Creates completion for the Event, Team, and Player rule options.</summary>
    public static ICompletionRange CreateEventCompletion(DocRange range, VanillaKeyword[] items) => ICompletionRange.New(
        range,
        getCompletionParams => items.Select(item => new CompletionItem()
        {
            Label = item.EnUs,
            Kind = CompletionItemKind.Constant
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