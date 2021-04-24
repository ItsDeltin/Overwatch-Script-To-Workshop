using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger
{
    public class MethodAttributes
    {
        /// <summary>The type the method belongs to.</summary>
        public CodeType ContainingType { get; set; }

        ///<summary>If true, the method can be called asynchronously.</summary>
        public bool Parallelable { get; set; } = false;

        ///<summary>If true, the method can be overriden.</summary>
        public bool Virtual { get; set; } = false;

        ///<summary>If true, the method must be overriden.</summary>
        public bool Abstract { get; set; } = false;

        ///<summary>If true, the method is overriding another method.</summary>
        public bool Override { get; set; } = false;

        /// <summary>The overriden method.</summary>
        public IMethod Overriding { get; set; }

        ///<summary>Determines if the method can be overriden. This will return true if the method is virtual, abstract, or overriding another method.</summary>
        public bool IsOverrideable => Virtual || Abstract || Override;

        /// <summary>Determines if the method was overriden.</summary>
        public bool WasOverriden => AllOverrideOptions().Length > 0;

        /// <summary>An array of methods that directly overrides the function. Call `AllOverrideOptions` instead for all child overriders.</summary>
        public IMethod[] Overriders => _overriders.ToArray();

        /// <summary>Determines if the method can be called recursively.</summary>
        public bool Recursive { get; set; }

        private readonly List<IMethod> _overriders = new List<IMethod>();

        public MethodAttributes() { }

        public MethodAttributes(bool isParallelable, bool isVirtual, bool isAbstract)
        {
            Parallelable = isParallelable;
            Virtual = isVirtual;
            Abstract = isAbstract;
        }

        public void AddOverride(IMethod overridingMethod)
        {
            _overriders.Add(overridingMethod);
        }

        public IMethod[] AllOverrideOptions()
        {
            List<IMethod> options = new List<IMethod>();

            options.AddRange(_overriders);

            foreach (var overrider in _overriders)
                options.AddRange(overrider.Attributes.AllOverrideOptions());

            return options.ToArray();
        }
    }

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

        public static string Message_Element(RestrictedCallType type) => $"A restricted value of type '{StringFromCallType(type)}' cannot be called in this rule.";
        public static string Message_EventPlayerDefault(string name)
            => $"The variable '{name}' is a player variable and no player was provided in a global rule.";
        public static string Message_FunctionCallsRestricted(string functionName, RestrictedCallType type)
            => $"The function '{functionName}' calls a restricted value of type '{RestrictedCall.StringFromCallType(type)}'.";
        public static string Message_Macro(string macroName, RestrictedCallType type)
            => $"The macro '{macroName}' calls a restricted value of type '{RestrictedCall.StringFromCallType(type)}'.";
        public static string Message_LambdaInvoke(string lambdaName, RestrictedCallType type)
            => $"The lambda '{lambdaName}' calls a restricted value of type '{RestrictedCall.StringFromCallType(type)}'.";
        public static string Message_UnsetOptionalParameter(string parameterName, string functionName, RestrictedCallType type)
            => $"An unset optional parameter '{parameterName}' in the function '{functionName}' calls a restricted value of type '{RestrictedCall.StringFromCallType(type)}'.";

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
        
        public static bool EventPlayerDefaultCall(IVariable provider, IExpression parent, ParseInfo parseInfo)
            => provider.VariableType == VariableType.Player && (parent == null || parent is RootAction);
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