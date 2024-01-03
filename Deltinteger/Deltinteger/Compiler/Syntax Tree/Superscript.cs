#nullable enable

using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.SyntaxTree;

public record class VanillaRule(Token Disabled, Token Name, VanillaRuleContent[] Content);

public record class VanillaRuleContent(Token GroupToken, IVanillaExpression[] InnerItems);

interface IVanillaRuleElement { }

record class VanillaRuleEvent : IVanillaRuleElement;

record class VanillaRuleConditions(IVanillaExpression[] Conditions) : IVanillaRuleElement;

record class VanillaRuleActions(IVanillaExpression[] Actions) : IVanillaRuleElement;

public interface IVanillaExpression { }

record class VanillaSymbolExpression(Token Token) : IVanillaExpression;

record class VanillaInvokeExpression(IVanillaExpression Invoking, List<IVanillaExpression> Arguments) : IVanillaExpression { }