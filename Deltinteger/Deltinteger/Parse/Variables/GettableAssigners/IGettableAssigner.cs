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
        /// <summary>Generates the IGettable from the specified parameters.</summary>
        GettableAssignerResult GetResult(GettableAssignerValueInfo info);
        /// <summary>Shorthand for GetResult(info).Value.</summary>
        IGettable GetValue(GettableAssignerValueInfo info) => GetResult(info).Gettable;
        /// <summary>Assigns the variable to a class stack. May return null if the variable will not 
        /// be stored in a class stack.</summary>
        IGettable AssignClassStacks(GetClassStacks info);
        IGettable Unfold(IUnfoldGettable unfolder);
        /// <summary>Gets the number of variables required to store this value type.</summary>
        int StackDelta();
        /// <summary>Gets the initial value of the variable.</summary>
        public (IWorkshopTree Value, bool HasDefault) GetInitialValue(GettableAssignerValueInfo info, IGettable bonusRegister);
    }

    /// <summary>Returned by IGettableAssigner.GetResult.</summary>
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

    /// <summary>Used by IGettableAssigner.AssignClassStacks. Tracks the current struct's stack
    /// delta when assigning gettables to a struct.</summary>
    public struct GetClassStacks
    {
        public IStackFromIndex StackData { get; }
        public int StackOffset { get; }

        public GetClassStacks(IStackFromIndex stackData, int stackOffset)
        {
            StackData = stackData;
            StackOffset = stackOffset;
        }
    }

    /// <summary>Gets a gettable stack via an index. This is used to store structs in class variables
    /// and flattening paralleled structs into unparalleled structs.</summary>
    public interface IStackFromIndex
    {
        IGettable StackFromIndex(int stackIndex);

        public static IStackFromIndex FromArray(IGettable[] stacks) => new StackList(stacks);

        record StackList(IGettable[] stacks) : IStackFromIndex
        {
            public IGettable StackFromIndex(int stackIndex) => stacks[stackIndex];
        }
    }

    public interface IUnfoldGettable
    {
        IWorkshopTree NextValue();
    }
}