namespace Deltin.Deltinteger.Parse
{
    public class GettableAssignerValueInfo
    {
        public ActionSet ActionSet { get; }
        public bool SetInitialValue { get; set; } = true;
        public IWorkshopTree InitialValueOverride { get; set; }
        public bool Inline { get; set; }
        public WorkshopVariableAssigner IndexReferenceCreator { get; set; }
        public bool IsGlobal { get; set; }

        public GettableAssignerValueInfo(
            ActionSet actionSet,
            bool setInitialValue,
            IWorkshopTree initialValue,
            bool inline,
            WorkshopVariableAssigner indexReferenceCreator,
            bool isGlobal)
        {
            ActionSet = actionSet;
            SetInitialValue = setInitialValue;
            InitialValueOverride = initialValue;
            Inline = inline;
            IndexReferenceCreator = indexReferenceCreator;
            IsGlobal = isGlobal;
        }

        public GettableAssignerValueInfo(ActionSet actionSet)
        {
            ActionSet = actionSet;
            IndexReferenceCreator = new WorkshopVariableAssigner(actionSet.VarCollection);
            IsGlobal = actionSet.IsGlobal;
        }

        public GettableAssignerValueInfo(VarCollection varCollection)
        {
            IndexReferenceCreator = new WorkshopVariableAssigner(varCollection);
            IsGlobal = true;
        }

        public GettableAssignerValueInfo(WorkshopVariableAssigner indexReferenceCreator)
        {
            IndexReferenceCreator = indexReferenceCreator;
            IsGlobal = true;
        }

        public static implicit operator GettableAssignerValueInfo(ActionSet actionSet) => new GettableAssignerValueInfo(actionSet);
    }
    
    public interface IGettableAssigner
    {
        GettableAssignerResult GetResult(GettableAssignerValueInfo info);
        IGettable GetValue(GettableAssignerValueInfo info) => GetResult(info).Gettable;
        IGettable AssignClassStacks(GetClassStacks info);
        IGettable Unfold(IUnfoldGettable unfolder);
        int StackDelta();
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
        public ClassWorkshopInitializerComponent ClassData { get; }
        public int StackOffset { get; }

        public GetClassStacks(ClassWorkshopInitializerComponent classData, int stackOffset)
        {
            ClassData = classData;
            StackOffset = stackOffset;
        }
    }

    public interface IUnfoldGettable
    {
        IWorkshopTree NextValue();
    }
}