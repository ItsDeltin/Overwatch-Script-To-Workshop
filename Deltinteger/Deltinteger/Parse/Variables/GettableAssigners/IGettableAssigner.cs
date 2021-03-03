using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class GettableAssignerValueInfo
    {
        public ActionSet ActionSet { get; }
        public VarCollection VarCollection { get; }
        public VarIndexAssigner Assigner { get; }
        public bool SetInitialValue { get; set; } = true;
        public IWorkshopTree InitialValueOverride { get; set; }
        public bool Inline { get; set; }

        public GettableAssignerValueInfo(ActionSet actionSet, VarCollection varCollection, VarIndexAssigner assigner)
        {
            ActionSet = actionSet;
            VarCollection = varCollection;
            Assigner = assigner;
        }

        public GettableAssignerValueInfo(ActionSet actionSet)
        {
            ActionSet = actionSet;
            VarCollection = actionSet.VarCollection;
            Assigner = actionSet.IndexAssigner;
        }

        public static implicit operator GettableAssignerValueInfo(ActionSet actionSet) => new GettableAssignerValueInfo(actionSet);
    }
    
    public interface IGettableAssigner
    {
        GettableAssignerResult GetResult(GettableAssignerValueInfo info);
        IGettable GetValue(GettableAssignerValueInfo info) => GetResult(info).Gettable;
        IGettable AssignClassStacks(GetClassStacks info);
        int StackDelta();
    }

    /// <summary>Assigner for normal variables.</summary>
    class DataTypeAssigner : IGettableAssigner
    {
        private readonly Var _var;

        public DataTypeAssigner(Var var)
        {
            _var = var;
        }

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            var inline = _var.StoreType == StoreType.None || info.Inline;

            // Get the initial value.
            IWorkshopTree initialValue = Element.Num(0);

            // Set the initial value to the override if it exists.
            if (info.InitialValueOverride != null)
                initialValue = info.InitialValueOverride;

            // Otherwise, use the var's initial value.
            else if (_var.InitialValue != null)
                initialValue = _var.InitialValue.Parse(info.ActionSet);

            // Inline
            if (inline) return new GettableAssignerResult(new WorkshopElementReference(initialValue), initialValue);
            
            // Assign the index reference
            var value = info.VarCollection.Assign(_var.Name, _var.VariableType, info.ActionSet.IsGlobal, _var.InExtendedCollection, _var.ID);

            // Add the variable to the assigner.
            // info.Assigner.Add(_var, value);

            // Set the initial value.
            if (_var.Settable() && info.SetInitialValue)
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
    }

    /// <summary>Assigner for constant workshop values.</summary>
    class ConstantWorkshopValueAssigner : IGettableAssigner
    {
        private readonly ExpressionOrWorkshopValue _value;

        public ConstantWorkshopValueAssigner(ExpressionOrWorkshopValue value) => _value = value;
        public ConstantWorkshopValueAssigner(IWorkshopTree value) => _value = new ExpressionOrWorkshopValue(value);
        public ConstantWorkshopValueAssigner(IExpression value) => _value = new ExpressionOrWorkshopValue(value);

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            var value = _value.Parse(info.ActionSet);
            return new GettableAssignerResult(new WorkshopElementReference(value), value);
        }
        public IGettable AssignClassStacks(GetClassStacks info) => throw new System.NotImplementedException();
        public int StackDelta() => 0;
    }

    public class GettableAssignerResult
    {
        public IGettable Gettable { get; }
        public IWorkshopTree InitialValue { get; }

        public GettableAssignerResult(IGettable gettable, IWorkshopTree initialValue)
        {
            Gettable = gettable;
            InitialValue = initialValue;
        }
    }

    public class GetClassStacks
    {
        public DeltinScript DeltinScript { get; }
        public int StackOffset { get; }
        public ClassWorkshopInitializerComponent ClassData { get; }

        public GetClassStacks(DeltinScript deltinScript, int stackOffset)
        {
            DeltinScript = deltinScript;
            StackOffset = stackOffset;
            ClassData = DeltinScript.GetComponent<ClassWorkshopInitializerComponent>();
        }
    }
}