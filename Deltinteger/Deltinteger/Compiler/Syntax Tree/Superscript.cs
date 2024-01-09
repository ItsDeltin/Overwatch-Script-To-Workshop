#nullable enable

using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

public record class VanillaVariableCollection(DocRange Range, List<GroupOrName> Items);

public readonly struct GroupOrName
{
    public readonly VariableGroup? Group = null;
    public readonly VariableName? Name = null;

    public GroupOrName(VariableGroup? group) => Group = group;
    public GroupOrName(VariableName? name) => Name = name;
}

public record struct VariableGroup(Token GroupToken);

public record struct VariableName(Token Id, Token? Name);

public record class VanillaRule(Token Disabled, Token Name, VanillaRuleContent[] Content);

public record class VanillaRuleContent(DocRange Range, Token GroupToken, IVanillaExpression[] InnerItems);

public interface IVanillaExpression
{
    DocRange Range { get; }
}

record class VanillaSymbolExpression(Token Token) : IVanillaExpression
{
    public DocRange Range => Token;
}

record class VanillaInvokeExpression(DocRange Range, IVanillaExpression Invoking, List<VanillaInvokeParameter> Arguments, Token LeftParentheses, Token? RightParentheses) : IVanillaExpression;

record struct VanillaInvokeParameter(Token PreceedingComma, IVanillaExpression Value);

record class MissingVanillaExpression(DocRange Range) : IVanillaExpression;

record class VanillaBinaryOperatorExpression(IVanillaExpression Left, Token Symbol, IVanillaExpression Right) : IVanillaExpression
{
    public DocRange Range => Left.Range.Start + Right.Range.End;
}

record class ParenthesizedVanillaExpression(DocRange Range, IVanillaExpression Value) : IVanillaExpression;

record class VanillaStringExpression(Token Token) : IVanillaExpression
{
    public DocRange Range => Token;
}