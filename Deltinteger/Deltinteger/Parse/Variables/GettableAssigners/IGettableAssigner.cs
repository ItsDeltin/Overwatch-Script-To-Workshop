namespace Deltin.Deltinteger.Parse
{
    public class GettableAssignerValueInfo
    {
        public ActionSet ActionSet { get; }
        public VarCollection VarCollection { get; }
        public bool SetInitialValue { get; set; } = true;
        public IWorkshopTree InitialValueOverride { get; set; }
        public bool Inline { get; set; }
        public WorkshopVariableAssigner IndexReferenceCreator { get; set; }
        public bool IsGlobal { get; set; }

        public GettableAssignerValueInfo(
            ActionSet actionSet,
            VarCollection varCollection,
            bool setInitialValue,
            IWorkshopTree initialValue,
            bool inline,
            WorkshopVariableAssigner indexReferenceCreator,
            bool isGlobal)
        {
            ActionSet = actionSet;
            VarCollection = varCollection;
            SetInitialValue = setInitialValue;
            InitialValueOverride = initialValue;
            Inline = inline;
            IndexReferenceCreator = indexReferenceCreator;
            IsGlobal = isGlobal;
        }

        public GettableAssignerValueInfo(ActionSet actionSet, VarCollection varCollection, VarIndexAssigner assigner)
        {
            ActionSet = actionSet;
            VarCollection = varCollection;
            IndexReferenceCreator = new WorkshopVariableAssigner(varCollection);
            IsGlobal = actionSet.IsGlobal;
        }

        public GettableAssignerValueInfo(ActionSet actionSet)
        {
            ActionSet = actionSet;
            VarCollection = actionSet.VarCollection;
            IndexReferenceCreator = new WorkshopVariableAssigner(actionSet.VarCollection);
            IsGlobal = actionSet.IsGlobal;
        }

        public GettableAssignerValueInfo(VarCollection varCollection)
        {
            VarCollection = varCollection;
            IndexReferenceCreator = new WorkshopVariableAssigner(varCollection);
            IsGlobal = true;
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
}