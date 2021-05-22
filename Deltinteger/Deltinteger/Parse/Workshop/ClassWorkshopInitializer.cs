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
        IndexReference[] _stacks; // The object variables.
        int _newClassID; // Counts up from 0 assigning classes identifiers.
        readonly HashSet<WorkshopInitializedCombo> _initializedCombos = new HashSet<WorkshopInitializedCombo>();
        readonly List<ClassProviderComboCollection> _providerComboCollections = new List<ClassProviderComboCollection>();
        readonly List<ClassWorkshopRelation> _relations = new List<ClassWorkshopRelation>();

        public ClassWorkshopInitializerComponent(ToWorkshop toWorkshop)
        {
            _toWorkshop = toWorkshop;

            // Init classes then assign stacks.
            InitClasses();
            AssignStacks();
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

        public ClassWorkshopRelation RelationFromClassType(ClassType type) => _relations.First(relation => relation.Instance.Is(type));

        public void AddRelation(ClassWorkshopRelation relation) => _relations.Add(relation);

        ClassProviderComboCollection CollectionFromProvider(IClassInitializer provider) => _providerComboCollections.First(p => p.Provider == provider);

        // Assigns ObjectVariable stacks.
        void AssignStacks()
        {
            _stacks = new IndexReference[_stackCount];

            for (int i = 0; i < _stacks.Length; i++)
                _stacks[i] = _toWorkshop.DeltinScript.VarCollection.Assign(ObjectVariableTag + i, true, false);
        }

        public IndexReference ObjectVariableFromIndex(int i) => _stacks[i];
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
        public TypeArgCombo Combo { get; }
        public int ID { get; }
        public WorkshopInitializedCombo ExtendsCombo { get; private set; }
        public int StackOffset { get; private set; }
        public int StackLength { get; }

        readonly ClassWorkshopInitializerComponent _initializer;
        readonly ClassType _instance;

        public WorkshopInitializedCombo(ClassWorkshopInitializerComponent initializer, ClassType instance, TypeArgCombo combo, int id)
        {
            Combo = combo;
            ID = id;
            _initializer = initializer;
            _instance = instance;
            // StackLength = _instance.Attributes.StackLength;
            StackLength = instance.Variables.Select(v => v.GetAssigner(null).StackDelta()).Sum();
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

        public void AddVariableInstancesToAssigner(IVariableInstance[] instances, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            int stack = StackOffset;
            for (int i = 0; i < instances.Length; i++)
            {                
                var gettableAssigner = instances[i].GetAssigner(null);

                assigner.Add(
                    instances[i].Provider,
                    gettableAssigner.AssignClassStacks(new GetClassStacks(_initializer, stack)).ChildFromClassReference(reference)
                );

                stack += gettableAssigner.StackDelta();
            }
        }
    }

    public class ClassWorkshopRelation
    {
        public WorkshopInitializedCombo Combo { get; }
        public ClassType Instance { get; }
        public ClassWorkshopRelation Extending { get; private set; }
        readonly List<ClassWorkshopRelation> _extendedBy = new List<ClassWorkshopRelation>();

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

        public IEnumerable<ClassWorkshopRelation> GetAllExtenders()
        {
            foreach (var extender in _extendedBy)
            {
                yield return extender;
                foreach (var recursiveExtender in extender.GetAllExtenders())
                    yield return recursiveExtender;
            }
        }
    }
}