using System;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    abstract class ChaseMethod : CustomMethodBase
    {
        public VariableChaseData GetChaseData(IndexedVar var, TranslateRule context)
        {
            var existingChaseData = context.ParserData.Chasing.FirstOrDefault(cd => cd.Var == var);
            if (existingChaseData != null)
                return existingChaseData;
            
            IndexedVar destination = context.VarCollection.AssignVar(null, $"'{var.Name}' chase destination", var.IsGlobal, null);
            IndexedVar rate        = context.VarCollection.AssignVar(null, $"'{var.Name}' chase duration"   , var.IsGlobal, null);

            VariableChaseData newChaseData = new VariableChaseData(var, destination, rate);
            context.ParserData.Chasing.Add(newChaseData);

            Rule chaseRule = new Rule(
                Constants.INTERNAL_ELEMENT + "Chase Variable: " + var.Name, 
                var.IsGlobal ? RuleEvent.OngoingGlobal : RuleEvent.OngoingPlayer
            );
            chaseRule.Conditions = new Condition[]
            {
                new Condition(
                    rate.GetVariable(),
                    Operators.NotEqual,
                    new V_Number(0)
                )
            };
            chaseRule.Actions = ArrayBuilder<Element>.Build(
                UpdateVariable(var, destination, rate),
                A_Wait.MinimumWait,
                new A_LoopIfConditionIsTrue()
            );
            context.ParserData.AdditionalRules.Add(chaseRule);

            return newChaseData;
        }

        private static Element[] UpdateVariable(IndexedVar var, IndexedVar destination, IndexedVar rate)
        {
            Element rateAdjusted = Element.Part<V_Multiply>(rate.GetVariable(), new V_Number(Constants.MINIMUM_WAIT));

            Element distance = Element.Part<V_DistanceBetween>(var.GetVariable(), destination.GetVariable());

            Element ratio = Element.Part<V_Divide>(rateAdjusted, distance);

            Element delta = Element.Part<V_Subtract>(destination.GetVariable(), var.GetVariable());

            Element result = Element.TernaryConditional(
                new V_Compare(distance, Operators.GreaterThan, rateAdjusted),
                Element.Part<V_Add>(var.GetVariable(), Element.Part<V_Multiply>(ratio, delta)),
                destination.GetVariable()
            );

            return var.SetVariable(result);
        }
    }

    public class VariableChaseData
    {
        public readonly IndexedVar Var;
        public readonly IndexedVar Destination;
        public readonly IndexedVar Rate;

        public VariableChaseData(IndexedVar var, IndexedVar destination, IndexedVar rate)
        {
            Var = var;
            Destination = destination;
            Rate = rate;
        }
    }

    [CustomMethod("ChaseVariable", CustomMethodType.Action)]
    [VarRefParameter("Variable")]
    [Parameter("Destination", ValueType.Number | ValueType.Vector, null)]
    [Parameter("Rate", ValueType.Number, null)]
    class ChaseVariable : ChaseMethod
    {
        protected override MethodResult Get()
        {
            VarRef targetVariable = (VarRef)Parameters[0];
            
            if (targetVariable.Var is IndexedVar == false)
                throw SyntaxErrorException.InvalidVarRefType(targetVariable.Var.Name, VarType.Indexed, ParameterLocations[0]);

            IndexedVar var = (IndexedVar)targetVariable.Var;
            Element destination = (Element)Parameters[1];
            Element rate = (Element)Parameters[2];
            
            VariableChaseData chaseData = GetChaseData(var, TranslateContext);
            
            Element[] actions = ArrayBuilder<Element>.Build
            (
                chaseData.Destination.SetVariable(destination, targetVariable.Target),
                chaseData.Rate.SetVariable(rate, targetVariable.Target)
            );

            return new MethodResult(actions, null);
        }
    
        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Chases a variable to a value. Works with numbers and vectors.",
                // Parameters
                /* Variable    */ "Variable that will chase the destination.", 
                /* Destination */ "The final variable destination. Can be a number or vector.",
                /* Rate        */ "The chase speed per second."
            );
        }
    }

    [CustomMethod("StopChasingVariable", CustomMethodType.Action)]
    [VarRefParameter("Variable")]
    class StopChasingVariable : ChaseMethod
    {
        protected override MethodResult Get()
        {
            VarRef targetVariable = (VarRef)Parameters[0];

            if (targetVariable.Var is IndexedVar == false)
                throw SyntaxErrorException.InvalidVarRefType(targetVariable.Var.Name, VarType.Indexed, ParameterLocations[0]);

            IndexedVar var = (IndexedVar)targetVariable.Var;
            
            VariableChaseData chaseData = GetChaseData(var, TranslateContext);
            
            Element[] actions = ArrayBuilder<Element>.Build
            (
                chaseData.Rate.SetVariable(new V_Number(0), targetVariable.Target)
            );

            return new MethodResult(actions, null);
        }
    
        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Stops chasing a variable.", 
                // Parameters
                "Variable that will no longer be chasing."
            );
        }
    }
}