using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse.Lambda
{
    class LambdaToRule : IMethod
    {
        // public static FuncMethod Function = new FuncMethodBuilder() {
        //     Name = "ToRule",
        //     Documentation = "Converts the lambda to a rule.",
        //     Parameters = new CodeParameter[] {
        //         new ConstStringParameter("ruleName", "The name of the rule."),
        //         new ToRuleConditionParameter("condition", "The rule's conditions."),
        //         new CodeParameter("eventType", "The type of the event.", ValueGroupType.GetEnumType<RuleEvent>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(RuleEvent.OngoingGlobal))),
        //         new CodeParameter("teamType", "The team that the rule applies to.", ValueGroupType.GetEnumType<Team>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(Team.All))),
        //         new CodeParameter("playerType", "The players that the rule applies to.", ValueGroupType.GetEnumType<PlayerSelector>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(PlayerSelector.All))),
        //         new ConstNumberParameter("sortOrder", "The order of the rule.", 0)
        //     },
        //     Action = (actionSet, methodCall) => {
        //         // Get the rule data.
        //         string ruleName       = (string)methodCall.AdditionalParameterData[0];
        //         RuleEvent eventType   = (RuleEvent)((EnumMember)methodCall.ParameterValues[2]).Value;
        //         Team team             = (Team)((EnumMember)methodCall.ParameterValues[3]).Value;
        //         PlayerSelector player = (PlayerSelector)((EnumMember)methodCall.ParameterValues[4]).Value;

        //         // Get the conditions.
        //         ToRuleUnfoldResolver conditionInfo = (ToRuleUnfoldResolver)methodCall.AdditionalParameterData[1];

        //         // Convert the conditions.
        //         Condition[] conditions = new Condition[conditionInfo.Conditions.Length];
        //         for (int i = 0; i < conditions.Length; i++)
        //             conditions[i] = new Condition((Element)conditionInfo.Conditions[i].Parse(actionSet));

        //         // Create the rule.
        //         TranslateRule newRule = new TranslateRule(actionSet.Translate.DeltinScript, ruleName, eventType, team, player);
        //         newRule.Conditions.AddRange(conditions);

        //         // Translate the lambda.
        //         ((LambdaAction)actionSet.CurrentObject).Invoke(newRule.Actions, );

        //         actionSet.Translate.DeltinScript.WorkshopRules.Add();
        //         return null;
        //     }
        // };

        public string Name => "ToRule";
        public string Documentation => "Converts the lambda to a rule.";
        public CodeType ReturnType => null;
        public bool DoesReturnValue => false;
        public bool Static => false;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public CodeParameter[] Parameters { get; }
        private readonly int _invokeParametersEnd;

        public LambdaToRule(CodeType[] argumentTypes)
        {
            // Get invoke arg parameters.
            List<CodeParameter> parameters = new List<CodeParameter>();
            parameters.AddRange(LambdaInvoke.ParametersFromTypes(argumentTypes));
            _invokeParametersEnd = parameters.Count;

            // Add additional parameters.
            parameters.AddRange(new CodeParameter[] {
                new ConstStringParameter("ruleName", "The name of the rule."),
                new ToRuleConditionParameter("condition", "The rule's conditions."),
                new CodeParameter("eventType", "The type of the event.", ValueGroupType.GetEnumType<RuleEvent>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(RuleEvent.OngoingGlobal))),
                new CodeParameter("teamType", "The team that the rule applies to.", ValueGroupType.GetEnumType<Team>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(Team.All))),
                new CodeParameter("playerType", "The players that the rule applies to.", ValueGroupType.GetEnumType<PlayerSelector>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(PlayerSelector.All))),
                new ConstNumberParameter("sortOrder", "The order of the rule.", 0)
            });

            Parameters = parameters.ToArray();
        }

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => HoverHandler.GetLabel("void", Name, Parameters, markdown, Documentation);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Get the rule data.
            string ruleName       = (string)methodCall.AdditionalParameterData[_invokeParametersEnd];
            RuleEvent eventType   = (RuleEvent)((EnumMember)methodCall.ParameterValues[_invokeParametersEnd + 2]).Value;
            Team team             = (Team)((EnumMember)methodCall.ParameterValues[_invokeParametersEnd + 3]).Value;
            PlayerSelector player = (PlayerSelector)((EnumMember)methodCall.ParameterValues[_invokeParametersEnd + 4]).Value;

            // Get the conditions.
            ToRuleUnfoldResolver conditionInfo = (ToRuleUnfoldResolver)methodCall.AdditionalParameterData[_invokeParametersEnd + 1];

            // Convert the conditions.
            Condition[] conditions = new Condition[conditionInfo.Conditions.Length];
            for (int i = 0; i < conditions.Length; i++)
                conditions[i] = new Condition((Element)conditionInfo.Conditions[i].Parse(actionSet));

            // Create the rule.
            TranslateRule newRule = new TranslateRule(actionSet.Translate.DeltinScript, ruleName, eventType, team, player);

            // Set the priority.
            newRule.Priority = (double)methodCall.AdditionalParameterData[_invokeParametersEnd + 5];

            // Add the conditions.
            newRule.Conditions.AddRange(conditions);

            // Translate the lambda.
            ((LambdaAction)actionSet.CurrentObject).Invoke(newRule.ActionSet, methodCall.ParameterValues.Take(_invokeParametersEnd).ToArray());

            // Convert the rule.
            actionSet.Translate.DeltinScript.WorkshopRules.Add(newRule.GetRule());
            return null;
        }
    }

    class ToRuleConditionParameter : CodeParameter
    {
        public ToRuleConditionParameter(string name, string documentation) : base(name, documentation) {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            // Create the unfold resolver.
            var unfoldResolver = new ToRuleUnfoldResolver();

            // Resolve the expression.
            ConstantExpressionResolver.Resolve(value, resolve => {
                // If the resolved value is a CreateArrayAction, each condition will be each value in the array.
                if (resolve is CreateArrayAction createArrayAction)
                    unfoldResolver.Conditions = createArrayAction.Values;
                // No conditions if null.
                if (resolve is NullAction)
                    unfoldResolver.Conditions = new IExpression[0];
                
                // If nothing special is found, the condition will just be the resolved value.
                // If needed, this can just be changed to be 'expression' if that works better.
                else unfoldResolver.Conditions = new IExpression[] { resolve };
            });
            return unfoldResolver;
        }
        
        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }

    class ToRuleUnfoldResolver
    {
        public IExpression[] Conditions { get; set; }
    }
}