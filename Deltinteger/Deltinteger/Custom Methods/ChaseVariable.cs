using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("ChaseVariableAtRate", "Gradually modifies the value of a variable at a specific rate.", CustomMethodType.Action, typeof(NullType))]
    public class ChaseVariableAtRate : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new VariableParameter("variable", "The variable to manipulate. Player variables will chase the event player's variable. Must be a variable defined on the rule level.", VariableType.Dynamic, new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true }),
            new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values."),
            new CodeParameter("rate", "The amount of change that will happen to the variable’s value each second."),
            new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", ValueGroupType.GetEnumType("RateChaseReevaluation"))
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            VariableElements elements = ((VariableResolve)additionalParameterData[0]).ParseElements(actionSet);
            WorkshopVariable variable = elements.IndexReference.WorkshopVariable;

            Element destination = (Element)parameterValues[1];
            Element rate = (Element)parameterValues[2];
            IWorkshopTree reevaluation = parameterValues[3];
            
            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part("Chase Global Variable At Rate",
                    variable,
                    destination,
                    rate,
                    reevaluation
                ));
            else
                actionSet.AddAction(Element.Part("Chase Player Variable At Rate",
                    elements.Target,
                    variable,
                    destination,
                    rate,
                    reevaluation
                ));

            return null;
        }
    }

    [CustomMethod("ChaseVariableOverTime", "Gradually modifies the value of a variable over time.", CustomMethodType.Action, typeof(NullType))]
    public class ChaseVariableOverTime : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new VariableParameter("variable", "The variable to manipulate. Player variables will chase the event player's variable. Must be a variable defined on the rule level.", VariableType.Dynamic, new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true }),
            new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values."),
            new CodeParameter("duration", "The amount of time, in seconds, over which the variable's value will approach the destination."),
            new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", ValueGroupType.GetEnumType("TimeChaseReevaluation"))
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            VariableElements elements = ((VariableResolve)additionalParameterData[0]).ParseElements(actionSet);
            WorkshopVariable variable = elements.IndexReference.WorkshopVariable;

            Element destination = (Element)parameterValues[1];
            Element duration = (Element)parameterValues[2];
            IWorkshopTree reevaluation = parameterValues[3];
            
            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part("Chase Global Variable Over Time",
                    variable,
                    destination,
                    duration,
                    reevaluation
                ));
            else
                actionSet.AddAction(Element.Part("Chase Player Variable Over Time",
                    elements.Target,
                    variable,
                    destination,
                    duration,
                    reevaluation
                ));

            return null;
        }
    }

    [CustomMethod("StopChasingVariable", "Stops an in-progress chase of a variable, leaving it at its current value.", CustomMethodType.Action, typeof(NullType))]
    public class StopChasingVariable : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new VariableParameter("variable", "The variable to stop. Must be a variable defined on the rule level.", VariableType.Dynamic, new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true })
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            VariableElements elements = ((VariableResolve)additionalParameterData[0]).ParseElements(actionSet);
            WorkshopVariable variable = elements.IndexReference.WorkshopVariable;

            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part("Stop Chasing Global Variable", variable));
            else
                actionSet.AddAction(Element.Part("Stop Chasing Player Variable", elements.Target, variable));

            return null;
        }
    }
}
