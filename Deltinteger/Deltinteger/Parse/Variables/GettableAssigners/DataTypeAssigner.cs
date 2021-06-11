using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    /// <summary>Assigner for normal variables.</summary>
    class DataTypeAssigner : IGettableAssigner
    {
        readonly AssigningAttributes _attributes;

        public DataTypeAssigner(AssigningAttributes attributes)
        {
            _attributes = attributes;
        }

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            var inline = _attributes.StoreType == StoreType.None || info.Inline;

            // Get the initial value.
            IWorkshopTree initialValue = Element.Num(0);

            // Set the initial value to the override if it exists.
            if (info.InitialValueOverride != null)
                initialValue = info.InitialValueOverride;

            // Otherwise, use the var's initial value.
            else if (_attributes.DefaultValue != null)
                initialValue = _attributes.DefaultValue.Parse(info.ActionSet);

            // Inline
            if (inline) return new GettableAssignerResult(new WorkshopElementReference(initialValue), initialValue);
            
            // Assign the index reference
            var value = info.IndexReferenceCreator.Create(_attributes);

            // Set the initial value.
            if (info.SetInitialValue)
            {
                if (value is RecursiveIndexReference recursive)
                {
                    info.ActionSet.InitialSet().AddAction(recursive.Reset());
                    info.ActionSet.AddAction(recursive.Push((Element)initialValue));
                }
                else
                    info.ActionSet.AddAction(value.SetVariable((Element)initialValue));
            }

            return new GettableAssignerResult(value, initialValue);
        }
    
        public IGettable AssignClassStacks(GetClassStacks info) =>
            info.ClassData.ObjectVariableFromIndex(info.StackOffset);

        public int StackDelta() => 1;

        public IGettable Unfold(IUnfoldGettable unfolder) => new WorkshopElementReference(unfolder.NextValue());
    }

    public struct AssigningAttributes
    {
        public static readonly AssigningAttributes Empty = new AssigningAttributes();

        public string Name;
        public VariableType VariableType;
        public StoreType StoreType;
        public bool IsGlobal;
        public bool Extended;
        public int ID;
        public IExpression DefaultValue;

        public AssigningAttributes(string name, bool isGlobal, bool extended)
        {
            Name = name;
            VariableType = VariableType.Dynamic;
            StoreType = extended ? StoreType.Indexed : StoreType.FullVariable;
            IsGlobal = isGlobal;
            Extended = extended;
            ID = -1;
            DefaultValue = null;
        }
    }
}