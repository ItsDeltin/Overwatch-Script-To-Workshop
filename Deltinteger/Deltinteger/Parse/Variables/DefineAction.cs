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
            Assign(actionSet, DefiningVariable);
        }

        public static void Assign(ActionSet actionSet, Var var)
        {
            // Get the initial value.
            IWorkshopTree initialValue = Element.Num(0);
            if (var.InitialValue != null)
                initialValue = var.InitialValue.Parse(actionSet);

            // Add the variable to the assigner.
            actionSet.IndexAssigner.Add(actionSet.VarCollection, var, actionSet.IsGlobal, initialValue);

            // Set the initial value.
            if (var.Settable())
            {
                IndexReference reference = (IndexReference)actionSet.IndexAssigner[var];

                if (reference is RecursiveIndexReference recursive)
                {
                    actionSet.InitialSet().AddAction(recursive.Reset());
                    actionSet.AddAction(recursive.Push((Element)initialValue));
                }
                else
                    actionSet.AddAction(reference.SetVariable((Element)initialValue));
            }
        }
    }
}