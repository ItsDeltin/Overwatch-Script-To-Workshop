#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Vanilla.Ide;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Model;
using SignatureHelp = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureHelp;

namespace Deltin.Deltinteger.Parse.Vanilla;

using static VanillaHelper;

interface IVanillaNode
{
    public DocRange DocRange();
    public VanillaType Type();
    public Result<IWorkshopTree, string> GetWorkshopElement();
    public NodeSymbolInformation GetSymbolInformation();

    public static IVanillaNode New(IVanillaExpression node, Func<Result<IWorkshopTree, string>> getWorkshopElement)
        => new VanillaExpression(node.Range, new(), getWorkshopElement);

    public static IVanillaNode New(
        IVanillaExpression node,
        NodeSymbolInformation symbolInformation,
        Func<Result<IWorkshopTree, string>> getWorkshopElement)
        => new VanillaExpression(node.Range, symbolInformation, getWorkshopElement);

    record class VanillaExpression(
        DocRange Range,
        NodeSymbolInformation SymbolInformation,
        Func<Result<IWorkshopTree, string>> GetWorkshopElementFunc) : IVanillaNode
    {
        public DocRange DocRange() => Range;
        public Result<IWorkshopTree, string> GetWorkshopElement() => GetWorkshopElementFunc();
        public VanillaType Type()
        {
            throw new NotImplementedException();
        }
        public NodeSymbolInformation GetSymbolInformation() => SymbolInformation;
    }
}

record struct NodeSymbolInformation(ElementBaseJson? WorkshopFunction, bool DoNotError);

static class VanillaExpressions
{
    /// <summary>Creates a node for nodes contained in parentheses.</summary>
    public static IVanillaNode Grouped(VanillaContext context, ParenthesizedVanillaExpression syntax)
    {
        var item = VanillaAnalysis.AnalyzeExpression(context, syntax.Value);
        return IVanillaNode.New(syntax, () => item.GetWorkshopElement());
    }

    /// <summary>Creates a number node.</summary>
    public static IVanillaNode Number(VanillaContext context, NumberExpression syntax)
    {
        return IVanillaNode.New(syntax, () => Element.Num(syntax.Value));
    }

    /// <summary>Creates a string node.</summary>
    public static IVanillaNode String(VanillaContext context, VanillaStringExpression syntax)
    {
        // String literals can only be used inside the 'String' and 'Custom String' values.
        if (!context.GetActiveParameterData().NeedsStringLiteral)
        {
            context.Error("String literal cannot be used here", syntax.Range);
        }

        return IVanillaNode.New(syntax, () => "Bad string accepted, should be handled by parent function");
    }

    /// <summary>Creates a node representing a predefined workshop function or player-defined variable or subroutine</summary>
    public static IVanillaNode Symbol(VanillaContext context, VanillaSymbolExpression syntax)
    {
        void UnknownSymbol()
        {
            // For better diagnostics, search the context for the variable.
            var variableInScope = context.ScopedVanillaVariables.GetScopedVariableOfAnyType(syntax.Token.Text);
            if (variableInScope.HasValue)
            {
                string varType = GlobalOrPlayerString(variableInScope.Value.IsGlobal);
                string varName = variableInScope.Value.Name;
                context.Warning($"Unknown workshop symbol. Did you mean to reference the {varType} variable '{varName}'?", syntax.Range);
            }
            else
            {
                context.Warning("Unknown workshop symbol or variable", syntax.Range);
            }
        }

        ElementBaseJson? workshopFunction = null;
        ElementEnumMember? workshopConstant = null;

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
                    context.Hint("This symbol is ambiguous, translating to languages outside of en-US may be inaccurate", syntax.Range);
                }

                var selected = items[0];

                switch (selected)
                {
                    case WorkshopItem.ActionValue element:
                        workshopFunction = element.Value;
                        // Add hover info.
                        context.AddHover(syntax.Range, VanillaCompletion.FunctionSignature(new(), element.Value));

                        // If this expression needs to be invoked and is not, add an error.
                        if (!context.GetActiveParameterData().IsInvoked && workshopFunction.Parameters?.Length is not null and > 0)
                            context.Warning($"'{workshopFunction.Name}' requires {workshopFunction.Parameters.Length} parameter values", syntax.Range);

                        break;

                    case WorkshopItem.Enumerator enumerator:
                        workshopConstant = enumerator.Member;
                        break;
                }
            }
        }
        else
        {
            UnknownSymbol();
        }

        return IVanillaNode.New(syntax, new() { WorkshopFunction = workshopFunction }, () => (workshopFunction, workshopConstant) switch
        {
            (ElementBaseJson, null) => Element.Part(workshopFunction),
            (null, ElementEnumMember) => workshopConstant,
            (null, null) or (_, _) => "Attempted to compile symbol information with incomplete data"
        });
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
        var invoking = VanillaAnalysis.AnalyzeExpression(context.SetActiveParameterData(new(IsInvoked: true)), syntax.Invoking);
        var symbolInformation = invoking.GetSymbolInformation();

        if (symbolInformation.WorkshopFunction is null && !symbolInformation.DoNotError)
        {
            context.Error("This expression cannot be invoked", syntax.Range);
        }

        var element = symbolInformation.WorkshopFunction;
        var elementParams = symbolInformation.WorkshopFunction?.Parameters;

        // Add parameter completion for constant values.
        AddCompletionForParameters(context, syntax, element);

        // Analyze arguments.
        var arguments = syntax.Arguments.Select(arg => VanillaAnalysis.AnalyzeExpression(context, arg.Value)).ToArray();

        if (elementParams is not null)
        {
            // Not enough arguments.
            if (arguments.Length < elementParams.Length)
            {
                context.Warning(
                    $"The argument '{elementParams[arguments.Length].Name}' is missing from {VanillaHelper.GetTypeOfWorkshopFunction(element!)} '{element!.Name}'",
                    syntax.Invoking.Range);
            }
            // Too many arguments
            else if (arguments.Length > elementParams.Length)
            {
                context.Warning(
                    $"{VanillaHelper.GetTypeOfWorkshopFunction(element!)} '{element!.Name}' takes {elementParams.Length} parameters; got {arguments.Length}",
                    syntax.Invoking.Range);
            }
            // Add signature help
            if (syntax.RightParentheses is not null)
            {
                context.AddSignatureInfo(ISignatureHelp.New(
                    range: syntax.LeftParentheses.Range + syntax.RightParentheses.Range,
                    getSignatureHelp: getHelpArgs => new SignatureHelp()
                    {
                        Signatures = new[] {
                            VanillaCompletion.GetFunctionSignatureInformation(
                                element!,
                                VanillaCompletion.GetActiveParameter(syntax, getHelpArgs.CaretPos))
                        }
                    }
                ));
            }
        }

        return IVanillaNode.New(syntax, () =>
        {
            return arguments.SelectResult(arg => arg.GetWorkshopElement()).AndThen<IWorkshopTree>(args =>
                symbolInformation.WorkshopFunction switch
                {
                    ElementBaseJson => Element.Part(symbolInformation.WorkshopFunction, args.ToArray()),
                    null => "Attempted to compile invocation with incomplete data"
                });
        });
    }

    static void AddCompletionForParameters(VanillaContext context, VanillaInvokeExpression syntax, ElementBaseJson? element)
    {
        var elementParams = element?.Parameters;
        if (elementParams is null) return;

        for (int i = 0; i < syntax.Arguments.Count; i++)
        {
            var arg = syntax.Arguments[i];

            // Add special completion for constants
            if (i < elementParams.Length &&
                ElementRoot.Instance.TryGetEnum(elementParams[i].Type, out var constantsGroup))
            {
                // start of argument
                DocPos start = (arg.PreceedingComma ?? syntax.LeftParentheses).Range.End;
                // end of argument
                DocPos? end = (syntax.Arguments.ElementAtOrDefault(i + 1).PreceedingComma ?? syntax.RightParentheses)?.Range.Start;

                if (end is not null)
                {
                    context.AddCompletion(ICompletionRange.New(start + end, CompletionRangeKind.ClearRest, getCompletionArgs =>
                        VanillaCompletion.GetConstantsCompletion(constantsGroup, arg.Value.Range)));
                }
            }
        }
    }

    public static IVanillaNode Binary(VanillaContext context, VanillaBinaryOperatorExpression syntax)
    {
        IVanillaNode left = VanillaAnalysis.AnalyzeExpression(context, syntax.Left);
        IVanillaNode right;
        // Target variable.
        if (syntax.Symbol.Text == ".")
        {
            // Player variable completion
            context.AddCompletion(ICompletionRange.New(
                range: syntax.Symbol.Range.End + context.NextToken(syntax.Symbol).Range.Start,
                CompletionRangeKind.ClearRest,
                getCompletionParams => VanillaCompletion.GetDeclaredVariableCompletion(
                    context.ScopedVanillaVariables,
                    isGlobal: false
                )
            ));
            // Variable analysis
            right = VanillaAnalysis.AnalyzeExpression(context, syntax.Right);
        }
        else
        {
            right = VanillaAnalysis.AnalyzeExpression(context, syntax.Right);
        }

        return IVanillaNode.New(syntax, () =>
        {
            var a = left.GetWorkshopElement();
            var b = right.GetWorkshopElement();
            return a.And(b).AndThen<IWorkshopTree>(ab => syntax.Symbol.Text switch
            {
                "-" => Element.Subtract(ab.a, ab.b),
                "+" => Element.Add(ab.a, ab.b),
                "*" => Element.Multiply(ab.a, ab.b),
                "/" => Element.Divide(ab.a, ab.b),
                _ => $"Unhandled binary operator: '{syntax.Symbol.Text}'"
            });
        });
    }
}