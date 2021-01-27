using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod ChaseVariableAtRate(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ChaseVariableAtRate",
            Documentation = "Gradually modifies the value of a variable at a specific rate.",
            Parameters = new CodeParameter[] {
                new VariableParameter("variable", "The variable to manipulate. Player variables will chase the event player's variable. Must be a variable defined on the rule level.", VariableType.Dynamic, deltinScript.Types.Any(), new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true }),
                new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values.", NumberOrVector(deltinScript)),
                new CodeParameter("rate", "The amount of change that will happen to the variable’s value each second.", deltinScript.Types.Number()),
                new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", deltinScript.Types.EnumType("RateChaseReevaluation"))
            },
            Action = (actionSet, methodCall) => {
                VariableElements elements = ((VariableResolve)methodCall.AdditionalParameterData[0]).ParseElements(actionSet);
                WorkshopVariable variable = ((IndexReference)elements.IndexReference).WorkshopVariable;

                Element destination = methodCall.Get(1);
                Element rate = methodCall.Get(2);
                IWorkshopTree reevaluation = methodCall.ParameterValues[3];
                
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
        };

        public static FuncMethod ChaseVariableOverTime(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ChaseVariableOverTime",
            Documentation = "Gradually modifies the value of a variable over time.",
            Parameters = new CodeParameter[] {
                new VariableParameter("variable", "The variable to manipulate. Player variables will chase the event player's variable. Must be a variable defined on the rule level.", VariableType.Dynamic, deltinScript.Types.Any(), new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true }),
                new CodeParameter("destination", "The value that the variable will eventually reach. The type of this value may be either a number or a vector, through the variable’s existing value must be of the same type before the chase begins. Can use number or vector based values.", NumberOrVector(deltinScript)),
                new CodeParameter("duration", "The amount of time, in seconds, over which the variable's value will approach the destination.", deltinScript.Types.Number()),
                new CodeParameter("reevaluation", "Specifies which of this action's inputs will be continuously reevaluated. This action will keep asking for and using new values from reevaluated inputs.", deltinScript.Types.EnumType("TimeChaseReevaluation"))
            },
            Action = (actionSet, methodCall) => {
                VariableElements elements = ((VariableResolve)methodCall.AdditionalParameterData[0]).ParseElements(actionSet);
                WorkshopVariable variable = ((IndexReference)elements.IndexReference).WorkshopVariable;

                Element destination = methodCall.Get(1);
                Element duration = methodCall.Get(2);
                IWorkshopTree reevaluation = methodCall.ParameterValues[3];
                
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
        };

        public static FuncMethod StopChasingVariable(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "StopChasingVariable",
            Documentation = "Stops an in-progress chase of a variable, leaving it at its current value.",
            Parameters = new CodeParameter[] {
                new VariableParameter("variable", "The variable to stop. Must be a variable defined on the rule level.", VariableType.Dynamic, deltinScript.Types.Any(), new VariableResolveOptions() { CanBeIndexed = false, FullVariable = true })
            },
            Action = (actionSet, methodCall) => {
                VariableElements elements = ((VariableResolve)methodCall.AdditionalParameterData[0]).ParseElements(actionSet);
                WorkshopVariable variable = ((IndexReference)elements.IndexReference).WorkshopVariable;

                if (variable.IsGlobal)
                    actionSet.AddAction(Element.Part("Stop Chasing Global Variable", variable));
                else
                    actionSet.AddAction(Element.Part("Stop Chasing Player Variable", elements.Target, variable));

                return null;
            }
        };

        private static CodeType NumberOrVector(DeltinScript deltinScript) => new PipeType(deltinScript.Types.Number(), deltinScript.Types.Vector());
    }
}