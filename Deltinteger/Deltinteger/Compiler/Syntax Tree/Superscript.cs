#nullable enable

using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

/// <summary>
/// variables {
///     global:
///         0: aGlobalVariable
///     player:
///         2: aWorkshopVariable
/// }</summary>
public record class VanillaVariableCollection(Token OpeningToken, DocRange Range, List<GroupOrName> Items)
{
    public bool IsSubroutineCollection() =>
        OpeningToken.TokenType == TokenType.WorkshopSubroutines ||
        OpeningToken.TokenType == TokenType.WorkshopSubroutinesEn;
}

/// <summary>In a workshop variables expression, represents a group (like global or player)
/// or a declared variable.</summary>
public readonly struct GroupOrName
{
    public readonly VariableGroup? Group = null;
    public readonly VariableName? Name = null;

    public GroupOrName(VariableGroup? group) => Group = group;
    public GroupOrName(VariableName? name) => Name = name;
}

/// <summary>
/// variables {
///     [global]:
///         0: aGlobalVariable
///     [player]:
///         2: aWorkshopVariable
/// }</summary>
public record struct VariableGroup(Token GroupToken);

/// <summary>
/// variables {
///     global:
///         [Id]: [Name]
///     player:
///         [Id]: [Name]
/// }</summary>
public record struct VariableName(Token Id, Token? Name)
{
    public readonly DocRange Range => Id.Range.Start + (Name ?? Id).Range.End;
}

/// <summary>
/// Syntax for vanilla workshop rules.
/// <code>
/// [Disabled] rule([Name]) {
///     [Content]
/// }
/// </code>
/// </summary>
public record class VanillaRule(Token Disabled, Token? Name, VanillaRuleContent[] Content);

/// <summary>The 'event', 'actions' or 'conditions' sections in a workshop rule.</summary>
public record class VanillaRuleContent(DocRange Range, Token GroupToken, CommentedVanillaExpression[] InnerItems);

/// <summary>A possibly commented action or condition.</summary>
public record struct CommentedVanillaExpression(Token? Comment, IVanillaExpression Expression, Token? Semicolon);

/// <summary>The node for expressions in the vanilla superscript.</summary>
public interface IVanillaExpression
{
    DocRange Range { get; }
}

/// <summary>A workshop action, value, constant, or a user-declared variable or subroutine.</summary>
record class VanillaSymbolExpression(Token Token) : IVanillaExpression
{
    public DocRange Range => Token;
}

/// <summary>Function call syntax in the vanilla superscript</summary>
record class VanillaInvokeExpression(DocRange Range, IVanillaExpression Invoking, List<VanillaInvokeParameter> Arguments, Token LeftParentheses, Token? RightParentheses) : IVanillaExpression;

/// <summary>A function call argument used by VanillaInvokeExpression.</summary>
record struct VanillaInvokeParameter(Token PreceedingComma, IVanillaExpression Value);

/// <summary>Placeholder when an expression is missing.</summary>
record class MissingVanillaExpression(DocRange Range) : IVanillaExpression;

/// <summary>Expressions joined by a binary operator. Includes dot, math, and boolean logic.</summary>
record class VanillaBinaryOperatorExpression(IVanillaExpression Left, Token Symbol, IVanillaExpression Right) : IVanillaExpression
{
    public DocRange Range => Left.Range.Start + Right.Range.End;
}

/// <summary>A vanilla expression wrapped in parentheses.</summary>
record class ParenthesizedVanillaExpression(DocRange Range, IVanillaExpression Value) : IVanillaExpression;

/// <summary>A string in the vanilla syntax.</summary>
record class VanillaStringExpression(Token Token) : IVanillaExpression
{
    public DocRange Range => Token;
}

/// <summary>Syntax for vanilla settings. Can be used as value or as the top-level settings group.</summary>
public record class VanillaSettingsGroupSyntax(DocRange Range, VanillaSettingSyntax[] Settings) : IVanillaSettingValueSyntax;

/// <summary>A workshop lobby setting.</summary>
public record class VanillaSettingSyntax(Token Name, Token? Colon, Token? TokenAfterColon, IVanillaSettingValueSyntax? Value);

/// <summary>The value of a 'VanillaSettingSyntax'.</summary>
public interface IVanillaSettingValueSyntax { }

/// <summary>A number value for a lobby setting.</summary>
record class NumberSettingSyntax(Token Value, Token? PercentSign) : IVanillaSettingValueSyntax;

/// <summary>A setting pointing to a setting symbol.</summary>
record class SymbolSettingSyntax(Token Symbol) : IVanillaSettingValueSyntax;