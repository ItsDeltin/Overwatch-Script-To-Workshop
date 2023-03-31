namespace Deltin.Deltinteger.Parse
{
    /// <summary>Parameters and restrictions given to an IGettableAssigner to generate an IGettable.</summary>
    public class GettableAssignerValueInfo
    {
        /// <summary>The current rule's action set. May be null.</summary>
        public ActionSet ActionSet { get; }

        /// <summary>Determines how the initial value is set.</summary>
        public SetInitialValue SetInitialValue { get; set; } = SetInitialValue.SetAndFallbackTo0;

        /// <summary>Provides an alternative initial value if not null.</summary>
        public IWorkshopTree InitialValueOverride { get; set; }

        /// <summary>Forces the generated gettable to be inlined if true.</summary>
        public bool Inline { get; set; }

        /// <summary>The object that can generate an IndexReference.</summary>
        public WorkshopVariableAssigner IndexReferenceCreator { get; set; }

        /// <summary>Should a global or player variable be generated?</summary>
        public bool IsGlobal { get; set; }

        /// <summary>If true, the generated IGettable will be a RecursiveIndexReference.</summary>
        public bool IsRecursive { get; set; }

        /// <summary>Will clear nonpersistent junk data even if SetInitialValue is DoNotSet.<br />
        /// Only relevant if reset_nonpersistent is set to true in the project's ds.toml file.</summary>
        public bool ForceNonpersistentClear { get; set; }

        public GettableAssignerValueInfo(
            ActionSet actionSet,
            SetInitialValue setInitialValue,
            IWorkshopTree initialValue,
            bool inline,
            WorkshopVariableAssigner indexReferenceCreator,
            bool isGlobal,
            bool isRecursive,
            bool forceNonpersistentClear)
        {
            ActionSet = actionSet;
            SetInitialValue = setInitialValue;
            InitialValueOverride = initialValue;
            Inline = inline;
            IndexReferenceCreator = indexReferenceCreator;
            IsGlobal = isGlobal;
            IsRecursive = isRecursive;
            ForceNonpersistentClear = forceNonpersistentClear;
        }

        public GettableAssignerValueInfo(ActionSet actionSet)
        {
            ActionSet = actionSet;
            IndexReferenceCreator = new WorkshopVariableAssigner(actionSet.VarCollection);
            IsGlobal = actionSet.IsGlobal;
            IsRecursive = actionSet.IsRecursive;
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

    public enum SetInitialValue
    {
        DoNotSet,
        SetIfExists,
        SetAndFallbackTo0
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