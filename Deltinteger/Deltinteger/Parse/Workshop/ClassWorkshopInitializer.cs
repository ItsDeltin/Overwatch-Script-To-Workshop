using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public class ClassWorkshopInitializerComponent
    {
        public const string ObjectVariableTag = "_objectVariable_";

        readonly ToWorkshop _toWorkshop;
        GlobTypeArgCollector _typeTracker => _toWorkshop.TypeArgGlob;
        int _stackCount; // The number of object variables that need to be assigned.
        int _newClassID; // Counts up from 0 assigning classes identifiers.
        readonly HashSet<WorkshopInitializedCombo> _initializedCombos = new HashSet<WorkshopInitializedCombo>();
        readonly List<ClassProviderComboCollection> _providerComboCollections = new List<ClassProviderComboCollection>();
        readonly List<ClassWorkshopRelation> _relations = new List<ClassWorkshopRelation>();
        public IndexReference[] Stacks { get; private set; } // The object variables.

        public ClassWorkshopInitializerComponent(ToWorkshop toWorkshop)
        {
            _toWorkshop = toWorkshop;

            // Init classes then assign stacks.
            InitClasses();
            AssignStacks();
            InitStacksIfResetNonpersistent();
        }

        // Assigns a unique class identifier.
        public int AssignID() => ++_newClassID;

        void InitClasses()
        {
            // Collect combos.
            foreach (var tracker in _typeTracker.Trackers)
                if (tracker.Key is ClassInitializer classProvider)
                    _providerComboCollections.Add(new ClassProviderComboCollection(this, classProvider, tracker.Value.TypeArgCombos));

            // Initialize combos.
            foreach (var comboCollection in _providerComboCollections)
                comboCollection.Init();

            // Link relations.
            foreach (var relations in _relations)
                relations.Link(this);
        }

        public void InitCombo(WorkshopInitializedCombo combo)
        {
            if (_initializedCombos.Add(combo))
            {
                // This if block will run if 'combo' was not yet initialized.
                combo.Init();
                _stackCount = Math.Max(_stackCount, combo.StackOffset + combo.StackLength);
            }
        }

        public WorkshopInitializedCombo ComboFromClassType(ClassType type)
        {
            // Get the combo collection from the providers.
            var providerCollection = CollectionFromProvider(type.Provider);

            // Get the first combo that is compatible with the input type.
            foreach (var initializedCombo in providerCollection.InitializedCombos)
                if (initializedCombo.Combo.CompatibleWith(type.Generics))
                    return initializedCombo;

            // No combos are compatible.
            throw new Exception("No combos are acceptable");
        }

        public ClassWorkshopRelation RelationFromClassType(ClassType type)
        {
            var relation = _relations.FirstOrDefault(relation => relation.Instance.Is(type));

            if (relation == null)
                throw new Exception("ClassWorkshopRelation is not created for type " + type.GetName());

            return relation;
        }

        public void AddRelation(ClassWorkshopRelation relation) => _relations.Add(relation);

        ClassProviderComboCollection CollectionFromProvider(IClassInitializer provider) => _providerComboCollections.First(p => p.Provider == provider);

        // Assigns ObjectVariable stacks.
        void AssignStacks()
        {
            Stacks = new IndexReference[_stackCount];

            for (int i = 0; i < Stacks.Length; i++)
                Stacks[i] = _toWorkshop.DeltinScript.VarCollection.Assign(ObjectVariableTag + i, true, false);
        }

        void InitStacksIfResetNonpersistent()
        {
            if (_toWorkshop.DeltinScript.Settings.ResetNonpersistent)
            {
                var initSet = _toWorkshop.DeltinScript.InitialGlobal.ActionSet;
                foreach (var stack in Stacks)
                    stack.Set(initSet, Elements.Element.Num(0));
            }
        }

        public IndexReference ObjectVariableFromIndex(int i) => Stacks[i];
    }

    public class ClassProviderComboCollection
    {
        public ClassInitializer Provider { get; }
        public IReadOnlyList<WorkshopInitializedCombo> InitializedCombos => _initializedCombos.AsReadOnly();
        readonly List<WorkshopInitializedCombo> _initializedCombos = new List<WorkshopInitializedCombo>();
        readonly ClassWorkshopInitializerComponent _initializer;

        public ClassProviderComboCollection(ClassWorkshopInitializerComponent initializer, ClassInitializer provider, IReadOnlyList<TypeArgCombo> combos)
        {
            Provider = provider;
            _initializer = initializer;

            foreach (var combo in combos)
            {
                // Instantiate the combo.
                var instance = (ClassType)Provider.GetInstance(new GetInstanceInfo(combo.TypeArgs));

                // No group was created yet.
                var group = GetCompatible(combo);
                if (group == null)
                {
                    group = new WorkshopInitializedCombo(initializer, instance, combo, initializer.AssignID());
                    _initializedCombos.Add(group);
                }

                // Create the relation.
                initializer.AddRelation(new ClassWorkshopRelation(group, instance));
            }
        }

        WorkshopInitializedCombo GetCompatible(TypeArgCombo combo) => _initializedCombos.FirstOrDefault(c => c.Combo.CompatibleWith(combo));

        public void Init()
        {
            foreach (var combo in _initializedCombos)
                _initializer.InitCombo(combo);
        }
    }

    public class WorkshopInitializedCombo
    {
        ///<summary>The source combo that this was initialized from.</summary>
        public TypeArgCombo Combo { get; }
        ///<summary>The unique ID of the combo. This can be used to identify compatible classes at workshop runtime.</summary>
        public int ID { get; }
        ///<summary>The initialized combo that this extends. May be null.</summary>
        public WorkshopInitializedCombo ExtendsCombo { get; private set; }
        ///<summary>The position where data is assigned in the class variable list. This will be 0 if ExtendsCombo == null.</summary>
        public int StackOffset { get; private set; }
        ///<summary>The number of variables that the class combo requires in order to store its data.</summary>
        public int StackLength { get; }

        readonly ClassWorkshopInitializerComponent _initializer;
        readonly ClassType _instance;

        public WorkshopInitializedCombo(ClassWorkshopInitializerComponent initializer, ClassType instance, TypeArgCombo combo, int id)
        {
            Combo = combo;
            ID = id;
            _initializer = initializer;
            _instance = instance;
            // todo
            // StackLength = _instance.Attributes.StackLength;
            StackLength = instance.Variables.Select(v => v.GetAssigner().StackDelta()).Sum();
        }

        public void Init()
        {
            // Add offset if the provider is overriding something.
            if (_instance.Extends != null)
            {
                ExtendsCombo = _initializer.ComboFromClassType((ClassType)_instance.Extends);
                StackOffset = ExtendsCombo.StackOffset + ExtendsCombo.StackLength;
            }
        }

        public void AddVariableInstancesToAssigner(IVariableInstance[] variables, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            var gettables = GetVariableGettables(variables, reference);
            foreach (var gettable in gettables)
                assigner.Add(gettable.Key.Provider, gettable.Value);
        }

        public IReadOnlyDictionary<IVariableInstance, IGettable> GetVariableGettables(IVariableInstance[] variables, IWorkshopTree reference)
        {
            var variablesToGettables = new Dictionary<IVariableInstance, IGettable>();

            // 'stack' represents an index in the list of class variables.
            int stack = StackOffset;
            foreach (var variable in variables)
            {
                // Get the gettable assigner.
                var gettableAssigner = variable.GetAssigner();

                // Create the gettable.
                var gettable = gettableAssigner.AssignClassStacks(new GetClassStacks(_initializer, stack))?.ChildFromClassReference(reference);

                if (gettable != null)
                {
                    variablesToGettables.Add(variable, gettable);
                    // Increase stack by the length of the gettable.
                    stack += gettableAssigner.StackDelta();
                }

            }

            return variablesToGettables;
        }
    }

    public class ClassWorkshopRelation
    {
        public WorkshopInitializedCombo Combo { get; }
        public ClassType Instance { get; }
        public ClassWorkshopRelation Extending { get; private set; }
        readonly List<ClassWorkshopRelation> _extendedBy = new List<ClassWorkshopRelation>();
        int _totalStackLength = -1;

        public ClassWorkshopRelation(WorkshopInitializedCombo combo, ClassType instance)
        {
            Combo = combo;
            Instance = instance;
        }

        public void Link(ClassWorkshopInitializerComponent initializer)
        {
            if (Instance.Extends != null)
            {
                Extending = initializer.RelationFromClassType((ClassType)Instance.Extends);
                Extending._extendedBy.Add(this);
            }
        }

        /// <summary>Recursively gets every class that extends this.</summary>
        public IEnumerable<ClassWorkshopRelation> GetAllExtenders()
        {
            foreach (var extender in _extendedBy)
            {
                yield return extender;
                foreach (var recursiveExtender in extender.GetAllExtenders())
                    yield return recursiveExtender;
            }
        }

        /// <summary>Extracts overriden elements that match a pattern.</summary>
        public IEnumerable<T> ExtractOverridenElements<T>(Func<T, bool> isMatch) where T : class, IScopeable
        {
            foreach (var extender in GetAllExtenders())
            {
                T value = extender.Instance.Elements.ScopeableElements.FirstOrDefault(element => element.Scopeable is T t && isMatch(t)).Scopeable as T;
                if (value != null)
                    yield return value;
            }
        }

        /// <summary>The number of object variables required to store this class and any extender.</summary> 
        public int GetTotalStackLength()
        {
            if (_totalStackLength == -1)
            {
                _totalStackLength = Combo.StackOffset + Combo.StackLength;
                if (_extendedBy.Count != 0)
                    _totalStackLength = Math.Max(_totalStackLength, _extendedBy.Max(extended => extended.GetTotalStackLength()));
            }
            return _totalStackLength;
        }
    }
}