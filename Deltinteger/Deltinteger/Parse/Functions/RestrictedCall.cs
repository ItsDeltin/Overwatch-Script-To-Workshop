using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger
{
    public class RestrictedCall
    {
        public static readonly Dictionary<RestrictedCallType, RuleEvent[]> SupportedGroups = new Dictionary<RestrictedCallType, RuleEvent[]> {
            {RestrictedCallType.Ability, new RuleEvent[] {
                RuleEvent.OnDamageDealt,
                RuleEvent.OnDamageTaken,
                RuleEvent.OnDeath,
                RuleEvent.OnElimination,
                RuleEvent.OnFinalBlow,
                RuleEvent.OnHealingDealt,
                RuleEvent.OnHealingTaken,
                RuleEvent.PlayerDealtKnockback,
                RuleEvent.PlayerReceivedKnockback
            }},
            {RestrictedCallType.Attacker, new RuleEvent[] {
                RuleEvent.OnDamageDealt,
                RuleEvent.OnDamageTaken,
                RuleEvent.OnDeath,
                RuleEvent.OnElimination,
                RuleEvent.OnFinalBlow,
                RuleEvent.PlayerDealtKnockback,
                RuleEvent.PlayerReceivedKnockback
            }},
            {RestrictedCallType.EventPlayer, new RuleEvent[] {
                RuleEvent.OnDamageDealt,
                RuleEvent.OnDamageTaken,
                RuleEvent.OnDeath,
                RuleEvent.OnElimination,
                RuleEvent.OnFinalBlow,
                RuleEvent.OngoingPlayer,
                RuleEvent.OnHealingDealt,
                RuleEvent.OnHealingTaken,
                RuleEvent.OnPlayerJoin,
                RuleEvent.OnPlayerLeave,
                RuleEvent.PlayerDealtKnockback,
                RuleEvent.PlayerReceivedKnockback,
                RuleEvent.Subroutine
            }},
            {RestrictedCallType.Healer, new RuleEvent[] {
                RuleEvent.OnHealingDealt,
                RuleEvent.OnHealingTaken
            }},
            {RestrictedCallType.Knockback, new RuleEvent[] {
                RuleEvent.PlayerDealtKnockback,
                RuleEvent.PlayerReceivedKnockback
            }}
        };

        public static RestrictedCallType? GetRestrictedCallTypeFromString(string value) => value switch
        {
            "Ability" => RestrictedCallType.Ability,
            "Attacker" => RestrictedCallType.Attacker,
            "Healer" => RestrictedCallType.Healer,
            "Knockback" => RestrictedCallType.Knockback,
            "Event Player" => RestrictedCallType.EventPlayer,
            _ => null
        };

        public static string Message_Element(RestrictedCallType type) => $"A restricted value of type '{StringFromCallType(type)}' cannot be called in this rule.";
        public static string Message_EventPlayerDefault(string name)
            => $"The variable '{name}' is a player variable and no player was provided in a global rule.";
        public static string Message_FunctionCallsRestricted(string functionName, RestrictedCallType type)
            => $"The function '{functionName}' calls a restricted value of type '{StringFromCallType(type)}'.";
        public static string Message_Macro(string macroName, RestrictedCallType type)
            => $"The macro '{macroName}' calls a restricted value of type '{StringFromCallType(type)}'.";
        public static string Message_LambdaInvoke(string lambdaName, RestrictedCallType type)
            => $"The lambda '{lambdaName}' calls a restricted value of type '{StringFromCallType(type)}'.";
        public static string Message_UnsetOptionalParameter(string parameterName, string functionName, RestrictedCallType type)
            => $"An unset optional parameter '{parameterName}' in the function '{functionName}' calls a restricted value of type '{StringFromCallType(type)}'.";

        public RestrictedCallType CallType { get; }
        public Location CallRange { get; }
        public string Message { get; }
        public bool Fatal { get; }

        public RestrictedCall(RestrictedCallType callType, Location callRange, string message, bool fatal = true)
        {
            CallType = callType;
            CallRange = callRange;
            Message = message;
            Fatal = fatal;
        }

        public void AddDiagnostic(FileDiagnostics diagnostics)
        {
            if (Fatal)
                diagnostics.Error(Message, CallRange.range);
            else
                diagnostics.Warning(Message, CallRange.range);
        }

        public static string StringFromCallType(RestrictedCallType type)
        {
            switch (type)
            {
                case RestrictedCallType.EventPlayer: return "Event Player";
                default: return type.ToString();
            }
        }

        public static void BridgeMethodCall(ParseInfo parseInfo, CallInfo callInfo, DocRange range, string name, bool fatal)
            => parseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.PostContent, () =>
            {
                // Collect the restricted calls.
                foreach (var restrictedCallType in CallInfoVisitor.CollectRestrictedCalls(callInfo))
                {
                    // Add the restricted call to the current CallInfo.
                    parseInfo.RestrictedCallHandler.AddRestrictedCall(new RestrictedCall(
                        restrictedCallType,
                        parseInfo.GetLocation(range),
                        RestrictedCall.Message_FunctionCallsRestricted(name, restrictedCallType),
                        fatal
                    ));
                }
            });
    }

    public enum RestrictedCallType
    {
        EventPlayer,
        Attacker,
        Healer,
        Knockback,
        Ability
    }
}