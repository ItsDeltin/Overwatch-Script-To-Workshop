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
using Deltin.WorkshopString;

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

record struct NodeSymbolInformation(
    ElementBaseJson? WorkshopFunction = default,
    ElementEnumMember? WorkshopConstant = default,
    string? SymbolName = default,
    bool DoNotError = false,
    bool IsVariable = false,
    VanillaVariable? PointingToVariable = default,
    bool IsGlobalSymbol = false,
    StringFunctionType StringFunctionType = default,
    string? StringLiteralValue = null);

enum StringFunctionType
{
    NotAString,
    String,
    CustomString
}

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
            context.Error("String literal cannot be used here, did you mean to use Custom String?", syntax.Range);
        }

        string value = WorkshopStringUtility.WorkshopStringFromRawText(syntax.Token.Text);

        return IVanillaNode.New(
            syntax,
            new(StringLiteralValue: value),
            () => "Bad string accepted, should be handled by parent function");
    }

    /// <summary>Creates a node representing a predefined workshop function or player-defined variable or subroutine</summary>
    public static IVanillaNode Symbol(VanillaContext context, VanillaSymbolExpression syntax)
    {
        string name = syntax.Token.Text;

        void UnknownSymbol()
        {
            // For better diagnostics, search the context for the variable.
            var variableInScope = context.ScopedVariables.GetScopedVariableOfAnyType(name);
            if (variableInScope.HasValue)
            {
                string varType = GlobalOrPlayerString(variableInScope.Value.IsGlobal);
                context.Warning($"Unknown workshop symbol. Did you mean to reference the {varType} variable '{name}'?", syntax.Range);
            }
            else
            {
                context.Warning("Unknown workshop symbol", syntax.Range);
            }
        }

        ElementBaseJson? workshopFunction = null;
        ElementEnumMember? workshopConstant = null;
        VanillaVariable? declaredVariable = null;
        VanillaSubroutine? subroutine = null;
        bool isVariable = false;
        bool isGlobalSymbol = false;
        StringFunctionType stringFunctionType = default;

        var parameterData = context.GetActiveParameterData();
        // User declared variable
        if (parameterData.ExpectingVariable != ExpectingVariable.None)
        {
            isVariable = true;
            bool isGlobalVarExpected = parameterData.ExpectingVariable == ExpectingVariable.Global;

            // Find variable with name
            declaredVariable = context.ScopedVariables.GetScopedVariable(name, isGlobalVarExpected);

            // Warn if not found
            if (declaredVariable is null)
            {
                context.Warning($"There is no {GlobalOrPlayerString(isGlobalVarExpected)} variable named '{name}'", syntax.Range);
            }
        }
        // Subroutine
        else if (parameterData.ExpectingSubroutine)
        {
            // Find subroutine with name
            subroutine = context.ScopedVariables.GetSubroutine(name);

            // Warn if not found
            if (subroutine is null)
            {
                context.Warning($"There is no subroutine named '{name}'", syntax.Range);
            }
        }
        // 'Global' symbol
        else if (VanillaInfo.GlobalNamespace.Match(name))
        {
            isGlobalSymbol = true;
        }
        // Workshop function or constant
        else if (syntax.Token is WorkshopToken workshopToken)
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

                        // String nodes
                        if (workshopFunction.Name == "String") stringFunctionType = StringFunctionType.String;
                        else if (workshopFunction.Name == "Custom String") stringFunctionType = StringFunctionType.CustomString;

                        // Add hover info.
                        context.AddHover(syntax.Range, VanillaCompletion.FunctionSignature(new(), element.Value));

                        // If this expression needs to be invoked and is not, add an error.
                        if (!parameterData.IsInvoked && workshopFunction.Parameters?.Length is not null and > 0)
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

        return IVanillaNode.New(syntax,
            symbolInformation: new()
            {
                WorkshopFunction = workshopFunction,
                WorkshopConstant = workshopConstant,
                SymbolName = name,
                PointingToVariable = declaredVariable,
                IsVariable = isVariable,
                IsGlobalSymbol = isGlobalSymbol,
                StringFunctionType = stringFunctionType
            },
            getWorkshopElement: () =>
            {
                // Function
                if (workshopFunction is not null)
                    return Element.Part(workshopFunction);
                // Constant
                else if (workshopConstant is not null)
                    return workshopConstant;
                // Subroutine
                else if (subroutine is not null)
                    return new Subroutine(0, subroutine.Value.Name);
                // Error
                else
                    return "Attempted to compile symbol information with incomplete data";
            });
    }

    static WorkshopItem[] FilterItemsFromContext(VanillaContext context, IEnumerable<LanguageLinkedWorkshopItem> items)
    {
        var parameterData = context.GetActiveParameterData();

        // Filter by type
        if (parameterData.ExpectingType is not null)
        {
            var filterTypes = items.Where(item => item.Item switch
            {
                // Filter constants
                WorkshopItem.Enumerator enumerator => context.VanillaTypeFromJsonName(enumerator.Member.Enum.Name) == parameterData.ExpectingType,
                _ => true
            });
            // Only accept if there are still items.
            if (filterTypes.Any())
                items = filterTypes;
        }

        // Filter by parameter count
        int? invokeParameterCount = parameterData.InvokeParameterCount;
        var filterParameters = items.Where(item => item.Item switch
        {
            // Argument count matches.
            WorkshopItem.ActionValue actionValue => invokeParameterCount.HasValue &&
                invokeParameterCount.Value == (actionValue.Value.Parameters?.Length ?? 0),
            // Enumerators should not be invoked.
            WorkshopItem.Enumerator enumerator => !invokeParameterCount.HasValue,
            _ => false
        });
        // Only accept if there are still items.
        if (filterParameters.Any())
            items = filterParameters;

        // Filter by language
        var likelyLanguages = context.LikelyLanguages();
        if (likelyLanguages is not null)
        {
            var filterLanguages = items.Where(item => likelyLanguages.Contains(item.Language));
            // Only accept if there are still items.
            if (filterLanguages.Any())
                items = filterLanguages;
        }

        // Get unique items.
        return items.Select(langItem => langItem.Item).Distinct().ToArray();
    }

    public static IVanillaNode Invoke(VanillaContext context, VanillaInvokeExpression syntax)
    {
        // Analyze invoked value
        var invoking = VanillaAnalysis.AnalyzeExpression(context.SetActiveParameterData(new(
            IsInvoked: true, InvokeParameterCount: syntax.Arguments.Count)), syntax.Invoking);

        var symbolInformation = invoking.GetSymbolInformation();

        if (symbolInformation.WorkshopFunction is null && !symbolInformation.DoNotError)
        {
            context.Error("This expression cannot be invoked", syntax.Range);
        }

        var element = symbolInformation.WorkshopFunction;
        var elementParams = symbolInformation.WorkshopFunction?.Parameters;

        // Custom string function
        if (symbolInformation.StringFunctionType != StringFunctionType.NotAString)
        {
            string? text = null;
            var stringArgs = Array.Empty<IVanillaNode>();

            // Make sure there is a string literal.
            if (syntax.Arguments.Count == 0)
            {
                context.Error("Missing string literal", syntax.RightParentheses);
            }
            else
            {
                // Analyze the string literal expression.
                var stringNode = VanillaAnalysis.AnalyzeExpression(
                    context.SetActiveParameterData(new(NeedsStringLiteral: true, IsInvoked: true)),
                    syntax.Arguments[0].Value);
                text = stringNode.GetSymbolInformation().StringLiteralValue;

                // Check if the first argument is a string literal.
                if (text is null)
                {
                    context.Error("Expected string literal", syntax.Arguments[0].Value.Range);
                }

                // Check argument count.
                if (syntax.Arguments.Count > 4)
                {
                    context.Warning("Strings can only have a max of 3 arguments", syntax.Arguments[4].Value.Range);
                }

                // Get args. Skip the string literal that was already analyzed.
                stringArgs = syntax.Arguments.Skip(1).Select(arg => VanillaAnalysis.AnalyzeExpression(context, arg.Value))
                    .ToArray();
            }
            // Return string node
            return IVanillaNode.New(syntax, () => stringArgs.SelectResult(arg => arg.GetWorkshopElement()).AndThen<IWorkshopTree>(args =>
                    new StringElement(
                        value: text,
                        localized: symbolInformation.StringFunctionType == StringFunctionType.String,
                        formats: args.ToArray())));
        }

        // Add parameter completion for constant values.
        AddCompletionForParameters(context, syntax, element);

        // Analyze arguments.
        var arguments = syntax.Arguments.Select((arg, i) =>
            VanillaAnalysis.AnalyzeExpression(GetContextForParameter(context, element, i), arg.Value)).ToArray();

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

        string elementName = element!.Name;

        for (int i = 0; i <= syntax.Arguments.Count; i++)
        {
            VanillaInvokeParameter? arg = i < syntax.Arguments.Count ? syntax.Arguments[i] : null;

            // start of argument
            DocPos start = (arg?.PreceedingComma ?? syntax.LeftParentheses).Range.End;
            // end of argument
            DocPos? end = (syntax.Arguments.ElementAtOrDefault(i + 1).PreceedingComma ?? syntax.RightParentheses)?.Range.Start;

            if (i < elementParams.Length && end is not null)
            {
                var range = start + end;

                // Add special completion for constants
                if (ElementRoot.Instance.TryGetEnum(elementParams[i].Type, out var constantsGroup))
                {
                    context.AddCompletion(ICompletionRange.New(range, CompletionRangeKind.ClearRest, getCompletionArgs =>
                        VanillaCompletion.GetConstantsCompletion(constantsGroup, arg?.Value.Range)));
                }
                // Add completion for items expecting a variable.
                else if (elementParams[i].IsVariableReference)
                {
                    bool isGlobal = elementParams[i].VariableReferenceIsGlobal ?? false;
                    context.AddCompletion(ICompletionRange.New(range, CompletionRangeKind.ClearRest, getCompletionArgs =>
                        VanillaCompletion.GetVariableCompletion(context.ScopedVariables, isGlobal)));
                }
                // Subroutines
                else if (DoesParameterNeedSubroutine(element, i))
                {
                    context.AddCompletion(VanillaCompletion.GetSubroutineCompletion(range, context.ScopedVariables));
                }
                // Add completion for values
                else
                {
                    // Get the notable values from the parameter type.
                    // Notable values will have a star next to their name.
                    var notableValuesForParameterType = context.VanillaTypeFromJsonName(elementParams[i].Type)
                        ?.NotableValues ?? Enumerable.Empty<string>();

                    context.AddCompletion(VanillaCompletion.GetValueCompletion(range, notableValuesForParameterType));
                }
            }
        }
    }

    static VanillaContext GetContextForParameter(VanillaContext context, ElementBaseJson? element, int parameter)
    {
        // Do not change context with incomplete parameter information. 
        if (element?.Parameters is null || parameter >= element.Parameters.Length)
            return context;

        var param = element.Parameters[parameter];

        return context.SetActiveParameterData(new(
            ExpectingVariable: param.VariableReferenceIsGlobal switch
            {
                true => ExpectingVariable.Global,
                false => ExpectingVariable.Player,
                null or _ => ExpectingVariable.None
            },
            ExpectingSubroutine: DoesParameterNeedSubroutine(element, parameter),
            ExpectingType: context.VanillaTypeFromJsonName(param.Type)));
    }

    static bool DoesParameterNeedSubroutine(ElementBaseJson element, int parameter)
    {
        return parameter == 0 && (element.Name == "Call Subroutine" || element.Name == "Start Rule");
    }

    public static IVanillaNode Binary(VanillaContext context, VanillaBinaryOperatorExpression syntax)
    {
        IVanillaNode left = VanillaAnalysis.AnalyzeExpression(context, syntax.Left);
        IVanillaNode right;
        // Target variable.
        if (syntax.Symbol.Text == ".")
        {
            bool isGlobal = left.GetSymbolInformation().IsGlobalSymbol;
            // Player variable completion
            context.AddCompletion(ICompletionRange.New(
                range: syntax.Symbol.Range.End + context.NextToken(syntax.Symbol).Range.Start,
                CompletionRangeKind.ClearRest,
                getCompletionParams => VanillaCompletion.GetVariableCompletion(
                    context.ScopedVariables,
                    isGlobal: isGlobal
                )
            ));
            // Variable analysis
            right = VanillaAnalysis.AnalyzeExpression(context.SetActiveParameterData(new(
                ExpectingVariable: isGlobal ? ExpectingVariable.Global : ExpectingVariable.Player
            )), syntax.Right);
            // Rhs *must* be a variable.
            if (!right.GetSymbolInformation().IsVariable)
            {
                context.Error("The righthand side of a dot must be a variable", syntax.Right.Range);
            }
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