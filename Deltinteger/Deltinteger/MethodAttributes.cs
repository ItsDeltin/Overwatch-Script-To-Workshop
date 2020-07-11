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

        ///<summary>Determines if the method can be overriden. This will return true if the method is virtual, abstract, or overriding another method.</summary>
        public bool IsOverrideable => Virtual || Abstract || Override;

        /// <summary>Determines if the method was overriden.</summary>
        public bool WasOverriden => AllOverrideOptions().Length > 0;

        /// <summary>An array of methods that directly overrides the function. Call `AllOverrideOptions` instead for all child overriders.</summary>
        public IMethod[] Overriders => _overriders.ToArray();

        /// <summary>Determines if the method can be called recursively.</summary>
        public bool Recursive { get; set; }

        private readonly List<IMethod> _overriders = new List<IMethod>();

        public MethodAttributes() {}

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

        public static CompletionItem GetFunctionCompletion(IMethod function) => new CompletionItem()
        {
            Label = function.Name,
            Kind = CompletionItemKind.Method,
            Detail = (!function.DoesReturnValue ? "void" : (function.ReturnType == null ? "define" : function.ReturnType.GetName())) + " " + function.GetLabel(false),
            Documentation = Extras.GetMarkupContent(function.Documentation)
        };
    }

    public class MethodCall
    {
        public IWorkshopTree[] ParameterValues { get; }
        public object[] AdditionalParameterData { get; }
        public CallParallel CallParallel { get; set; } = CallParallel.NoParallel;
        public string ActionComment { get; set; }

        public MethodCall(IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            ParameterValues = parameterValues;
            AdditionalParameterData = additionalParameterData;
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
        public RestrictedCallType CallType { get; }
        public Location CallRange { get; }
        public ICallStrategy CallStrategy { get; }

        public RestrictedCall(RestrictedCallType callType, Location callRange, ICallStrategy callStrategy)
        {
            CallType = callType;
            CallRange = callRange;
            CallStrategy = callStrategy;
        }

        public static string StringFromCallType(RestrictedCallType type)
        {
            switch (type)
            {
                case RestrictedCallType.EventPlayer: return "Event Player";
                default: return type.ToString();
            }
        }
        
        public static bool EventPlayerDefaultCall(IIndexReferencer referencer, ParseInfo parseInfo)
            => EventPlayerDefaultCall(referencer, parseInfo.SourceExpression, parseInfo);
        public static bool EventPlayerDefaultCall(IIndexReferencer referencer, IExpression parent, ParseInfo parseInfo)
            => referencer.VariableType == VariableType.Player && (parent == null || parent.ReturningScope() != parseInfo.TranslateInfo.PlayerVariableScope);
    }

    public interface ICallStrategy
    {
        string Message();
    }

    class CallStrategy : ICallStrategy
    {
        private readonly string _message;
        public CallStrategy(string message)
        {
            _message = message;
        }
        public string Message() => _message;
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