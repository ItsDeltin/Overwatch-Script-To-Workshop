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
            Detail = function.CodeType.GetNameOrVoid() + " " + function.Name + CodeParameter.GetLabels(function.Parameters),
            Documentation = Extras.GetMarkupContent(function.Documentation)
        };

        public static MarkupBuilder DefaultLabel(IMethod function)
        {
            MarkupBuilder markup = new MarkupBuilder()
                .StartCodeLine()
                .Add(function.CodeType.GetNameOrVoid())
                .Add(" ")
                .Add(function.Name + CodeParameter.GetLabels(function.Parameters))
                .EndCodeLine();
            
            if (function.Documentation != null)
            {
                markup
                    .NewSection()
                    .Add(function.Documentation);
            }
            
            return markup;
        }
    }

    public class MethodCall : Deltin.Deltinteger.Parse.FunctionBuilder.ICallHandler
    {
        public IWorkshopTree[] ParameterValues { get; }
        public object[] AdditionalParameterData { get; }
        public object AdditionalData { get; set; }
        public CallParallel ParallelMode { get; set; } = CallParallel.NoParallel;
        public string ActionComment { get; set; }

        public MethodCall(IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            ParameterValues = parameterValues;
            AdditionalParameterData = additionalParameterData;
        }

        public MethodCall(IWorkshopTree[] parameterValues)
        {
            ParameterValues = parameterValues;
            AdditionalParameterData = new object[parameterValues.Length];
        }

        /// <summary>Gets a parameter as an element.</summary>
        public Element Get(int i) => (Element)ParameterValues[i];
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

        public RestrictedCall(RestrictedCallType callType, Location callRange, string message)
        {
            CallType = callType;
            CallRange = callRange;
            Message = message;
        }

        public static string StringFromCallType(RestrictedCallType type)
        {
            switch (type)
            {
                case RestrictedCallType.EventPlayer: return "Event Player";
                default: return type.ToString();
            }
        }
        
        public static bool EventPlayerDefaultCall(IIndexReferencer referencer, IExpression parent, ParseInfo parseInfo)
            => referencer.VariableType == VariableType.Player && (parent == null || parent is RootAction);
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