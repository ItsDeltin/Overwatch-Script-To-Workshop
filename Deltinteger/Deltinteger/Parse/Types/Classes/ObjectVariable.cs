using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ObjectVariable
    {
        public IVariableInstance Variable { get; }
        public IGettable ArrayStore { get; private set; }
        public int StackCount { get; private set; } = 1;

        public ObjectVariable(IVariableInstance variable)
        {
            Variable = variable;
        }

        public void SetArrayStore(DeltinScript deltinScript, int stackOffset)
        {
            ArrayStore = Variable.GetAssigner().AssignClassStacks(new GetClassStacks(deltinScript, stackOffset));
        }

        public void AddToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(Variable.Provider, ArrayStore.ChildFromClassReference(reference));
        }

        public void Init(ActionSet actionSet, Element reference)
        {
            if (Variable is Var var && var.InitialValue != null)
                ArrayStore.Set(
                    actionSet,
                    value: (Element)var.InitialValue.Parse(actionSet),
                    target: null,
                    index: reference
                );
        }

        /// <summary>Gets the value from a reference.</summary>
        public IWorkshopTree Get(IWorkshopTree reference) => ArrayStore.ChildFromClassReference(reference).GetVariable();

        /// <summary>Gets the value from the current context's object reference.</summary>
        public Element Get(ActionSet actionSet) => (Element)Get(actionSet.CurrentObject);

        public void Set(ActionSet actionSet, Element reference, Element value)
        {
            actionSet.AddAction(((IndexReference)ArrayStore).SetVariable(value: value, index: reference));
        }
    }
}