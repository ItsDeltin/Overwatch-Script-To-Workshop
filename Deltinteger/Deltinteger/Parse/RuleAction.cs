using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class RuleAction
    {
        public string Name { get; }
        public bool Disabled { get; }
        public IExpression[] Conditions { get; }
        public IStatement Block { get; }

        public RuleEvent EventType { get; private set; }
        public Team Team { get; private set; }
        public PlayerSelector Player { get; private set; }
        public ElementCountCodeLens ElementCountLens { get; }

        public double Priority;

        public RuleAction(ParseInfo parseInfo, Scope scope, RuleContext ruleContext)
        {
            Name = ruleContext.Name;
            Disabled = ruleContext.Disabled != null;
            DocRange ruleInfoRange = ruleContext.RuleToken.Range;

            GetRuleSettings(parseInfo, scope, ruleContext);

            // Store restricted calls
            CallInfo callInfo = new CallInfo(parseInfo.Script);

            // Get the conditions.
            Conditions = new IExpression[ruleContext.Conditions.Count];
            for (int i = 0; i < Conditions.Length; i++)
            {
                // Make sure both left and right parentheses exists.
                if (ruleContext.Conditions[i].LeftParen && ruleContext.Conditions[i].RightParen)
                    parseInfo.Script.AddCompletionRange(new CompletionRange(
                        scope,
                        ruleContext.Conditions[i].LeftParen.Range + ruleContext.Conditions[i].RightParen.Range,
                        CompletionRangeKind.Catch
                    ));

                Conditions[i] = parseInfo.SetCallInfo(callInfo).GetExpression(scope, ruleContext.Conditions[i].Expression);
            }

            // Get the block.
            Block = parseInfo.SetCallInfo(callInfo).GetStatement(scope, ruleContext.Statement);

            // Check restricted calls.
            callInfo.CheckRestrictedCalls(EventType);

            // Get the rule order priority.
            if (ruleContext.Order != null)
                Priority = ruleContext.Order.Value;

            ElementCountLens = new ElementCountCodeLens(ruleInfoRange, parseInfo.TranslateInfo.OptimizeOutput);
            parseInfo.Script.AddCodeLensRange(ElementCountLens);
        }

        private void GetRuleSettings(ParseInfo parseInfo, Scope scope, RuleContext ruleContext)
        {
            RuleSetting teamContext = null, playerContext = null;
            bool setEventType = false, setTeam = false, setPlayer = false;

            foreach (var setting in ruleContext.Settings)
            {
                // Add completion.
                switch (setting.Setting.Text)
                {
                    case "Event": AddCompletion(parseInfo, setting.Dot, setting.Value, EventItems); break;
                    case "Team": AddCompletion(parseInfo, setting.Dot, setting.Value, TeamItems); break;
                    case "Player": AddCompletion(parseInfo, setting.Dot, setting.Value, PlayerItems); break;
                }

                // Get the value.
                if (setting.Value != null)
                {
                    var alreadySet = new Diagnostic("The " + setting.Setting.Text + " rule setting was already set.", setting.Range, Diagnostic.Error);
                    string name = setting.Value.Text;
                    DocRange range = setting.Value.Range;

                    switch (setting.Setting.Text)
                    {
                        case "Event":
                            if (setEventType) parseInfo.Script.Diagnostics.AddDiagnostic(alreadySet);
                            EventType = GetMember<RuleEvent>("Event", name, parseInfo.Script.Diagnostics, range);
                            setEventType = true;
                            break;

                        case "Team":
                            if (setTeam) parseInfo.Script.Diagnostics.AddDiagnostic(alreadySet);
                            Team = GetMember<Team>("Team", name, parseInfo.Script.Diagnostics, range);
                            setTeam = true;
                            teamContext = setting;
                            break;

                        case "Player":
                            if (setPlayer) parseInfo.Script.Diagnostics.AddDiagnostic(alreadySet);
                            Player = GetMember<PlayerSelector>("Player", name, parseInfo.Script.Diagnostics, range);
                            setPlayer = true;
                            playerContext = setting;
                            break;

                        default:
                            parseInfo.Script.Diagnostics.Error("Expected an enumerator of type 'Event', 'Team', or 'Player'.", setting.Setting.Range);
                            break;
                    }
                }
            }

            // Set the event type to player if the event type was not set and player or team was changed.
            if (!setEventType && ((setPlayer && Player != PlayerSelector.All) || (setTeam && Team != Team.All)))
                EventType = RuleEvent.OngoingPlayer;

            if (setEventType && EventType == RuleEvent.OngoingGlobal)
            {
                // Syntax error if the event type is global and the team type is not default.
                if (Team != Team.All)
                    parseInfo.Script.Diagnostics.Error("Can't change rule Team type with an event type of Ongoing Global.", teamContext.Range);
                // Syntax error if the event type is global and the player type is not default.
                if (Player != PlayerSelector.All)
                    parseInfo.Script.Diagnostics.Error("Can't change rule Player type with an event type of Ongoing Global.", playerContext.Range);
            }
        }

        private static T GetMember<T>(string groupName, string name, FileDiagnostics diagnostics, DocRange range)
        {
            foreach (var m in EnumData.GetEnum<T>().Members)
                if (name == m.CodeName)
                    return (T)m.Value;

            diagnostics.Error("Invalid " + groupName + " value.", range);
            return default(T);
        }

        /// <summary>Adds the completion for a rule setting.</summary>
        private static void AddCompletion(ParseInfo parseInfo, Token dot, Token value, CompletionItem[] items)
        {
            // Do nothing if there is no dot.
            if (dot == null || parseInfo.Script.IsTokenLast(dot)) return;

            // Add the completion.
            parseInfo.Script.AddCompletionRange(new CompletionRange(
                items,
                // Use the start of the next token if the value token is null.
                dot.Range.End + (value != null ? value.Range.End : parseInfo.Script.NextToken(dot).Range.Start),
                CompletionRangeKind.ClearRest
            ));
        }

        private static readonly CompletionItem[] EventItems = GetItems<RuleEvent>("Event");
        private static readonly CompletionItem[] TeamItems = GetItems<Team>("Team");
        private static readonly CompletionItem[] PlayerItems = GetItems<PlayerSelector>("Player");

        private static CompletionItem[] GetItems<T>(string tag) => EnumData.GetEnum<T>()
            .Members.Select(m => new CompletionItem()
            {
                Label = m.CodeName,
                Detail = m.CodeName,
                //Detail = new MarkupBuilder().StartCodeLine().Add(tag + "." + m.CodeName).ToString(),
                Kind = CompletionItemKind.Constant
            }).ToArray();
    }
}