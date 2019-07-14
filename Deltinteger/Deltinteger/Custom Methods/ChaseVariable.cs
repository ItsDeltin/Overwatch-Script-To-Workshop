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
            Element destination = (Element)Parameters[1];
            Element rate = (Element)Parameters[2];
            
            VariableChase chaseData = TranslateContext.ParserData.GetLooper(targetVariable.Var.IsGlobal).GetChaseData(targetVariable.Var, TranslateContext);
            
            Element[] actions = ArrayBuilder<Element>.Build
            (
                chaseData.Destination.SetVariable(destination, targetVariable.Target),
                chaseData.Rate.SetVariable(rate, targetVariable.Target)
            );

            return new MethodResult(actions, null);
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("ChaseVariable", "Chases a variable to a value. Works with numbers and vectors.", 
                new WikiParameter[]
                {
                    new WikiParameter("Variable", "Variable that will chase the destination."),
                    new WikiParameter("Destination", "The final variable destination. Can be a number or vector."),
                    new WikiParameter("Rate", "The chase speed per second.")
                }
            );
        }
    }
}