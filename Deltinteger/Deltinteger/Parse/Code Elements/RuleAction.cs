using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class RuleAction : CodeAction
    {
        public string Name { get; }
        public bool Disabled { get; }
        public RuleIfAction[] Conditions { get; }
        public BlockAction Block { get; }

        public RuleAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            Name = Extras.RemoveQuotes(ruleContext.STRINGLITERAL().GetText());
            Disabled = ruleContext.DISABLED() != null;

            // TODO: Event, Player, and Team enums.

            // Get the conditions
            if (ruleContext.rule_if() == null) Conditions = new RuleIfAction[0];
            else
            {
                Conditions = new RuleIfAction[ruleContext.rule_if().Length];
                for (int i = 0; i < Conditions.Length; i++)
                    Conditions[i] = new RuleIfAction(script, translateInfo, scope, ruleContext.rule_if(i));
            }

            Block = new BlockAction(script, translateInfo, scope.Child(), ruleContext.block());
        }
    }

    public class RuleIfAction : CodeAction
    {
        public IExpression Expression { get; }

        public RuleIfAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Rule_ifContext ifContext)
        {
            // Syntax error if there is no expression.
            if (ifContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ifContext.RIGHT_PAREN()));
            
            // Get the expression.
            else
                Expression = GetExpression(script, translateInfo, scope, ifContext.expr());
        }
    }
}