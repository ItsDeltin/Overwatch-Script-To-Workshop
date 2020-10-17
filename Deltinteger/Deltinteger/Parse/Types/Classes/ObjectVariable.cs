using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ObjectVariable
    {
        public IIndexReferencer Variable { get; }
        public IndexReference ArrayStore { get; private set; }

        public ObjectVariable(IIndexReferencer variable)
        {
            Variable = variable;
        }

        public void SetArrayStore(IndexReference store)
        {
            ArrayStore = store;
        }

        public void AddToAssigner(Element reference, VarIndexAssigner assigner)
        {
            assigner.Add(Variable, ArrayStore.CreateChild(reference));
        }

        public void Init(ActionSet actionSet, Element reference)
        {
            if (Variable is Var var && var.InitialValue != null)
            {
                actionSet.AddAction(ArrayStore.SetVariable(
                    value: (Element)var.InitialValue.Parse(actionSet),
                    index: reference
                ));
            }
        }

        /// <summary>Creates a direct reference to the ArrayStore.</summary>
        public IndexReference Spot(Element reference) => ArrayStore.CreateChild(reference);

        /// <summary>Gets the value from a reference.</summary>
        public Element Get(Element reference) => ArrayStore.Get()[reference];

        /// <summary>Gets the value from the current context's object reference.</summary>
        public Element Get(ActionSet actionSet) => Get((Element)actionSet.CurrentObject);

        public void Set(ActionSet actionSet, Element reference, Element value)
        {
            actionSet.AddAction(ArrayStore.SetVariable(value: value, index: reference));
        }
    }
}