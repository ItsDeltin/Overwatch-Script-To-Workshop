using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class DefineAction : IStatement
    {
        public Var DefiningVariable { get; }

        public DefineAction(Var var)
        {
            DefiningVariable = var;
        }

        public void Translate(ActionSet actionSet)
        {
            // Get the initial value.
            IWorkshopTree initialValue = new V_Number(0);
            if (DefiningVariable.InitialValue != null)
                initialValue = DefiningVariable.InitialValue.Parse(actionSet);
            
            // Add the variable to the assigner.
            actionSet.IndexAssigner.Add(actionSet.VarCollection, DefiningVariable, actionSet.IsGlobal, initialValue);

            // Set the initial value.
            if (DefiningVariable.Settable())
            {
                actionSet.AddAction(
                    ((IndexReference)actionSet.IndexAssigner[DefiningVariable]).SetVariable(
                        (Element)initialValue
                    )
                );
            }
        }
    }
}