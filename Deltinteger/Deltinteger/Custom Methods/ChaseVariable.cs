using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("ChaseVariable", CustomMethodType.Action)]
    [VarRefParameter("Variable")]
    [Parameter("Destination", ValueType.Number | ValueType.Vector, null)]
    [Parameter("Rate", ValueType.Number, null)]
    class ChaseVariable : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            VarRef targetVariable = (VarRef)Parameters[0];
            #warning will crash with model variable input.
            IndexedVar var = (IndexedVar)targetVariable.Var;
            Element destination = (Element)Parameters[1];
            Element rate = (Element)Parameters[2];
            
            VariableChase chaseData = TranslateContext.ParserData.GetLooper(var.IsGlobal).GetChaseData(var, TranslateContext);
            
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
    class StopChasingVariable : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            VarRef targetVariable = (VarRef)Parameters[0];
            #warning will crash with model variable input.
            IndexedVar var = (IndexedVar)targetVariable.Var;
            
            VariableChase chaseData = TranslateContext.ParserData.GetLooper(var.IsGlobal).GetChaseData(var, TranslateContext);
            
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