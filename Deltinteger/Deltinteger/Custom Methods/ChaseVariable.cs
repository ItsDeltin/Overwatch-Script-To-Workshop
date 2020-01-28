using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("ChaseVariableAtRate", "Gradually modifies the value of a variable at a specific rate.", CustomMethodType.Action)]
    public class ChaseVariableAtRate : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new WorkshopVariableParameter("variable", "The variable to manipulate. Player variables will chase the event player's variable. Must be a variable defined on the rule level.", VariableType.Dynamic),
            new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values."),
            new CodeParameter("rate", "The amount of change that will happen to the variable’s value each second."),
            new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", WorkshopEnumType.GetEnumType<RateChaseReevaluation>())
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            WorkshopVariable variable = (WorkshopVariable)parameterValues[0];
            Element destination = (Element)parameterValues[1];
            Element rate = (Element)parameterValues[2];
            IWorkshopTree reevaluation = parameterValues[3];
            
            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part<A_ChaseGlobalVariableAtRate>(
                    variable,
                    destination,
                    rate,
                    reevaluation
                ));
            else
                actionSet.AddAction(Element.Part<A_ChasePlayerVariableAtRate>(
                    new V_EventPlayer(),
                    variable,
                    destination,
                    rate,
                    reevaluation
                ));

            return null;
        }
    }

    [CustomMethod("ChaseVariableAtRate", "Gradually modifies the value of a player variable at a specific rate.", CustomMethodType.Action)]
    public class ChasePlayerVariableAtRate : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new WorkshopVariableParameter("variable", "The variable to manipulate. Must be a player variable defined on the rule level.", VariableType.Player),
            new CodeParameter("player", "The player whose variable will gradually change. If multiple players are provided, each of their variables will change independently."),
            new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values."),
            new CodeParameter("rate", "The amount of change that will happen to the variable’s value each second."),
            new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", WorkshopEnumType.GetEnumType<RateChaseReevaluation>())
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            WorkshopVariable variable = (WorkshopVariable)parameterValues[0];
            Element player = (Element)parameterValues[1];
            Element destination = (Element)parameterValues[2];
            Element rate = (Element)parameterValues[3];
            IWorkshopTree reevaluation = parameterValues[4];
            
            actionSet.AddAction(Element.Part<A_ChasePlayerVariableAtRate>(
                player,
                variable,
                destination,
                rate,
                reevaluation
            ));

            return null;
        }
    }

    [CustomMethod("ChaseVariableOverTime", "Gradually modifies the value of a variable over time.", CustomMethodType.Action)]
    public class ChaseVariableOverTime : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new WorkshopVariableParameter("variable", "The variable to manipulate. Player variables will chase the event player's variable. Must be a variable defined on the rule level.", VariableType.Dynamic),
            new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values."),
            new CodeParameter("duration", "The amount of time, in seconds, over which the variable's value will approach the destination."),
            new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", WorkshopEnumType.GetEnumType<TimeChaseReevaluation>())
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            WorkshopVariable variable = (WorkshopVariable)parameterValues[0];
            Element destination = (Element)parameterValues[1];
            Element duration = (Element)parameterValues[2];
            IWorkshopTree reevaluation = parameterValues[3];
            
            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part<A_ChaseGlobalVariableOverTime>(
                    variable,
                    destination,
                    duration,
                    reevaluation
                ));
            else
                actionSet.AddAction(Element.Part<A_ChasePlayerVariableOverTime>(
                    new V_EventPlayer(),
                    variable,
                    destination,
                    duration,
                    reevaluation
                ));

            return null;
        }
    }

    [CustomMethod("ChaseVariableOverTime", "Gradually modifies the value of a player variable over time.", CustomMethodType.Action)]
    public class ChasePlayerVariableOverTime : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new WorkshopVariableParameter("variable", "The variable to manipulate. Must be a player variable defined on the rule level.", VariableType.Player),
            new CodeParameter("player", "The player whose variable will gradually change. If multiple players are provided, each of their variables will change independently."),
            new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values."),
            new CodeParameter("duration", "The amount of time, in seconds, over which the variable's value will approach the destination."),
            new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", WorkshopEnumType.GetEnumType<TimeChaseReevaluation>())
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            WorkshopVariable variable = (WorkshopVariable)parameterValues[0];
            Element player = (Element)parameterValues[1];
            Element destination = (Element)parameterValues[2];
            Element duration = (Element)parameterValues[3];
            IWorkshopTree reevaluation = parameterValues[4];
            
            actionSet.AddAction(Element.Part<A_ChasePlayerVariableOverTime>(
                player,
                variable,
                destination,
                duration,
                reevaluation
            ));

            return null;
        }
    }

    [CustomMethod("StopChasingVariable", "Stops an in-progress chase of a variable, leaving it at its current value.", CustomMethodType.Action)]
    public class StopChasingVariable : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new WorkshopVariableParameter("variable", "The variable to stop. Must be a variable defined on the rule level.", VariableType.Dynamic)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            WorkshopVariable variable = (WorkshopVariable)parameterValues[0];

            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part<A_StopChasingGlobalVariable>(variable));
            else
                actionSet.AddAction(Element.Part<A_StopChasingPlayerVariable>(new V_EventPlayer(), variable));

            return null;
        }
    }

    [CustomMethod("StopChasingVariable", "Stops an in-progress chase of a player variable, leaving it at its current value.", CustomMethodType.Action)]
    public class StopChasingPlayerVariable : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new WorkshopVariableParameter("variable", "The variable to stop. Must be a player variable defined on the rule level.", VariableType.Player),
            new CodeParameter("player", "The player whose variable will stop changing. If multiple players are provided, each of their variables will stop changing."),
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            WorkshopVariable variable = (WorkshopVariable)parameterValues[0];
            Element player = (Element)parameterValues[1];

            actionSet.AddAction(Element.Part<A_StopChasingPlayerVariable>(player, variable));

            return null;
        }
    }
}