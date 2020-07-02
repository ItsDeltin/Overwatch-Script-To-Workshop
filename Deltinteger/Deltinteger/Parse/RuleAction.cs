using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class RuleAction
    {
        public string Name { get; }
        public bool Disabled { get; }
        public RuleIfAction[] Conditions { get; }
        public BlockAction Block { get; }
        
        public RuleEvent EventType { get; private set; }
        public Team Team { get; private set; }
        public PlayerSelector Player { get; private set; }
        public ElementCountCodeLens ElementCountLens { get; }
        private bool _setEventType;
        private bool _setTeam;
        private bool _setPlayer;

        public double Priority;
        private DocRange missingBlockRange;

        public RuleAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            Name = Extras.RemoveQuotes(ruleContext.STRINGLITERAL().GetText());
            Disabled = ruleContext.DISABLED() != null;
            DocRange ruleInfoRange = DocRange.GetRange(ruleContext.RULE_WORD());
            missingBlockRange = ruleInfoRange;

            GetRuleSettings(parseInfo, scope, ruleContext);

            // Store restricted calls
            CallInfo callInfo = new CallInfo(parseInfo.Script);

            // Get the conditions.
            if (ruleContext.rule_if() == null) Conditions = new RuleIfAction[0];
            else
            {
                Conditions = new RuleIfAction[ruleContext.rule_if().Length];
                for (int i = 0; i < Conditions.Length; i++)
                {
                    parseInfo.Script.AddCompletionRange(new CompletionRange(
                        scope,
                        DocRange.GetRange(ruleContext.rule_if(i).LEFT_PAREN(), ruleContext.rule_if(i).RIGHT_PAREN()),
                        CompletionRangeKind.Catch
                    ));

                    Conditions[i] = new RuleIfAction(parseInfo.SetCallInfo(callInfo), scope, ruleContext.rule_if(i));
                    missingBlockRange = DocRange.GetRange(ruleContext.rule_if(i));
                }
            }

            // Get the block.
            if (ruleContext.block() != null)
                Block = new BlockAction(parseInfo.SetCallInfo(callInfo), scope, ruleContext.block());
            else
                parseInfo.Script.Diagnostics.Error("Missing block.", missingBlockRange);
            
            // Check restricted calls.
            callInfo.CheckRestrictedCalls(EventType);
            
            // Get the rule order priority.
            if (ruleContext.number() != null)
                Priority = double.Parse(ruleContext.number().GetText());
            
            ElementCountLens = new ElementCountCodeLens(ruleInfoRange, parseInfo.TranslateInfo.OptimizeOutput);
            parseInfo.Script.AddCodeLensRange(ElementCountLens);
        }

        private void GetRuleSettings(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            DeltinScriptParser.ExprContext eventContext = null;
            DeltinScriptParser.ExprContext teamContext = null;
            DeltinScriptParser.ExprContext playerContext = null;

            foreach (var exprContext in ruleContext.expr())
            {
                missingBlockRange = DocRange.GetRange(exprContext);

                EnumValuePair enumSetting = (ExpressionTree.ResultingExpression(parseInfo.GetExpression(scope, exprContext)) as CallVariableAction)?.Calling as EnumValuePair;
                EnumData enumData = enumSetting?.Member.Enum;

                if (enumData == null || !ValidRuleEnums.Contains(enumData))
                    parseInfo.Script.Diagnostics.Error("Expected enum of type " + string.Join(", ", ValidRuleEnums.Select(vre => vre.CodeName)) + ".", DocRange.GetRange(exprContext));
                else
                {
                    var alreadySet = new Diagnostic("The " + enumData.CodeName + " rule setting was already set.", DocRange.GetRange(exprContext), Diagnostic.Error);

                    // Get the Event option.
                    if (enumData == EnumData.GetEnum<RuleEvent>())
                    {
                        if (_setEventType)
                            parseInfo.Script.Diagnostics.AddDiagnostic(alreadySet);
                        EventType = (RuleEvent)enumSetting.Member.Value;
                        _setEventType = true;
                        eventContext = exprContext;
                    }
                    // Get the Team option.
                    if (enumData == EnumData.GetEnum<Team>())
                    {
                        if (_setTeam)
                            parseInfo.Script.Diagnostics.AddDiagnostic(alreadySet);
                        Team = (Team)enumSetting.Member.Value;
                        _setTeam = true;
                        teamContext = exprContext;
                    }
                    // Get the Player option.
                    if (enumData == EnumData.GetEnum<PlayerSelector>())
                    {
                        if (_setPlayer)
                            parseInfo.Script.Diagnostics.AddDiagnostic(alreadySet);
                        Player = (PlayerSelector)enumSetting.Member.Value;
                        _setPlayer = true;
                        playerContext = exprContext;
                    }
                }
            }

            // Syntax error if changing the Team type when the Event type is set to Global.
            if (_setEventType && EventType == RuleEvent.OngoingGlobal)
            {
                if (Team != Team.All)
                    parseInfo.Script.Diagnostics.Error("Can't change rule Team type with an event type of Ongoing Global.", DocRange.GetRange(teamContext));
                if (Player != PlayerSelector.All)
                    parseInfo.Script.Diagnostics.Error("Can't change rule Player type with an event type of Ongoing Global.", DocRange.GetRange(playerContext));
            }
        }

        private static readonly EnumData[] ValidRuleEnums = new EnumData[]
        {
            EnumData.GetEnum<RuleEvent>(),
            EnumData.GetEnum<Team>(),
            EnumData.GetEnum<PlayerSelector>()
        };
    }

    public class RuleIfAction
    {
        public IExpression Expression { get; }

        public RuleIfAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Rule_ifContext ifContext)
        {
            // Syntax error if there is no expression.
            if (ifContext.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ifContext.RIGHT_PAREN()));
            
            // Get the expression.
            else
                Expression = parseInfo.GetExpression(scope, ifContext.expr());
        }
    }
}