using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

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
            bool hasDefaultValue = true;

            // Set the initial value to the override if it exists.
            if (info.InitialValueOverride != null)
                initialValue = info.InitialValueOverride;

            // Otherwise, use the var's initial value.
            else if (_attributes.DefaultValue != null)
                initialValue = _attributes.DefaultValue.Parse(info.ActionSet);

            // No default value
            else hasDefaultValue = false;

            // Inline
            if (inline) return new GettableAssignerResult(new WorkshopElementReference(initialValue), initialValue);

            // Assign the index reference
            var value = info.IndexReferenceCreator.Create(_attributes);

            // Make recursive if requested.
            if (info.IsRecursive) value = new RecursiveIndexReference(value);

            // If this is true, we can assume that the created value is not initialized to zero.
            bool resetUnpersistent = info.ActionSet?.DeltinScript.Settings.ResetUnpersistent ?? false;

            // Set persistent.
            if (_attributes.Persist)
            {
                InitIfUnset(info.ActionSet, value, (Element)initialValue);
            }
            // Set the initial value.
            else
            {
                if (info.SetInitialValue == SetInitialValue.SetAndFallbackTo0 || (info.SetInitialValue == SetInitialValue.SetIfExists && hasDefaultValue))
                {
                    if (value is RecursiveIndexReference recursive)
                    {
                        info.ActionSet.InitialSet().AddAction(recursive.Reset());
                        info.ActionSet.AddAction(recursive.Push((Element)initialValue));
                    }
                    else
                        info.ActionSet.AddAction(value.SetVariable((Element)initialValue));
                }
                else if (resetUnpersistent && info.SetInitialValue != SetInitialValue.DoNotSet)
                {
                    Init(info.ActionSet, value, (Element)initialValue);
                }
            }

            return new GettableAssignerResult(value, initialValue);
        }

        public IGettable AssignClassStacks(GetClassStacks info) =>
            info.ClassData.ObjectVariableFromIndex(info.StackOffset);

        public int StackDelta() => 1;

        public IGettable Unfold(IUnfoldGettable unfolder) => new WorkshopElementReference(unfolder.NextValue());

        /// <summary>Initializes an IndexReference to the specified value only if it wasn't set by an external source (eg. action copy)</summary>
        static void InitIfUnset(ActionSet actionSet, IndexReference ir, Element initialValue)
        {
            var initialSet = actionSet.InitialSet();
            initialSet.AddAction(If(Not(ir.Get())));
            ir.Set(initialSet, initialValue);
            initialSet.AddAction(End());
        }

        /// <summary>Initializes an IndexReference to the specified value.</summary>
        static void Init(ActionSet actionSet, IndexReference ir, Element initialValue)
        {
            var initialSet = actionSet.InitialSet();
            ir.Set(initialSet, initialValue);
        }
    }

    public struct AssigningAttributes
    {
        public static readonly AssigningAttributes Empty = new AssigningAttributes();

        public string Name;
        public VariableType VariableType;
        public StoreType StoreType;
        public bool IsGlobal;
        public bool Extended;
        public bool Persist;
        public int ID;
        public IExpression DefaultValue;

        public AssigningAttributes()
        {
            Name = null;
            VariableType = VariableType.Dynamic;
            StoreType = StoreType.None;
            IsGlobal = false;
            Extended = false;
            Persist = false;
            ID = 0;
            DefaultValue = null;
        }

        public AssigningAttributes(string name, bool isGlobal, bool extended)
        {
            Name = name;
            VariableType = VariableType.Dynamic;
            StoreType = extended ? StoreType.Indexed : StoreType.FullVariable;
            IsGlobal = isGlobal;
            Extended = extended;
            ID = -1;
            DefaultValue = null;
            Persist = false;
        }
    }
}