#nullable enable

using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

public record class VanillaRule(Token Disabled, Token Name, VanillaRuleContent[] Content);

public record class VanillaRuleContent(Token GroupToken, IVanillaExpression[] InnerItems);

public interface IVanillaExpression
{
    DocRange Range { get; }
}

record class VanillaSymbolExpression(Token Token) : IVanillaExpression
{
    public DocRange Range => Token;
}

record class VanillaInvokeExpression(DocRange Range, IVanillaExpression Invoking, List<IVanillaExpression> Arguments) : IVanillaExpression;

record class MissingVanillaExpression(DocRange Range) : IVanillaExpression;

record class VanillaBinaryOperatorExpression(IVanillaExpression Left, Token Symbol, IVanillaExpression Right) : IVanillaExpression
{
    public DocRange Range => Left.Range.Start + Right.Range.End;
}
