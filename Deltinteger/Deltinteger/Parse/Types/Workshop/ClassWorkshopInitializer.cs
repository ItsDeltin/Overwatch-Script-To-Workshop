using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    // TODO: Change IComponent to IWorkshopComponent
    public class ClassWorkshopInitializerComponent : IComponent
    {
        public const string ObjectVariableTag = "_objectVariable_";

        DeltinScript _deltinScript;
        TypeTrackerComponent _typeTracker;
        int _stackCount; // The number of object variables that need to be assigned.
        IndexReference[] _stacks;
        readonly Dictionary<IClassInitializer, WorkshopInitializedClass> _initialized = new Dictionary<IClassInitializer, WorkshopInitializedClass>();

        public void Init(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
            _typeTracker = deltinScript.GetComponent<TypeTrackerComponent>();

            // Init classes then assign stacks.
            InitClasses();
            AssignStacks();
        }

        // Initializes all classes in the tracker.
        void InitClasses()
        {
            foreach (var tracker in _typeTracker.Trackers)
                // TODO: Do not cast.
                if (tracker.Key is IClassInitializer classProvider)
                    InitClassProvider(classProvider, tracker.Value);
        }

        // Initializes a class.
        WorkshopInitializedClass InitClassProvider(IClassInitializer provider, ProviderTrackerInfo info)
        {
            // Check if the provider value already exists.
            if (_initialized.TryGetValue(provider, out var wicExisting))
                return wicExisting;

            int stackOffset = 0;

            // Add offset if the provider is overriding something.
            if (provider.Extends != null)
            {
                var extends = InitClassProvider(provider.Extends.Provider, _typeTracker.Trackers[provider]);
                stackOffset = extends.Offset + extends.StackLength;
            }

            // Create the WorkshopInitializedClass.
            var wic = new WorkshopInitializedClass(_deltinScript, provider, info, stackOffset);

            // Add to _initialized
            _initialized.Add(provider, wic);

            // Set _stackCount
            _stackCount = Math.Max(_stackCount, wic.Offset + wic.StackLength);

            return wic;
        }

        // Assigns ObjectVariable stacks.
        void AssignStacks()
        {
            _stacks = new IndexReference[_stackCount];

            for (int i = 0; i < _stacks.Length; i++)
                _stacks[i] = _deltinScript.VarCollection.Assign(ObjectVariableTag + i, true, false);
        }

        public WorkshopInitializedClass InitializedClassFromProvider(IClassInitializer provider) => _initialized[provider];
        public IndexReference ObjectVariableFromIndex(int i) => _stacks[i];
    }

    public class WorkshopInitializedClass
    {
        readonly DeltinScript _deltinScript;
        public IClassInitializer Provider { get; }
        public ProviderTrackerInfo Info { get; }
        public int Offset { get; }
        public int[] StackDeltas { get; }
        public int StackLength { get; }

        public WorkshopInitializedClass(
            DeltinScript deltinScript,
            IClassInitializer provider,
            ProviderTrackerInfo info,
            int stackOffset
        )
        {
            _deltinScript = deltinScript;
            Provider = provider;
            Info = info;
            Offset = stackOffset;

            StackDeltas = provider.ObjectVariables.Select(ov => GetObjectVariableStackCount(ov)).ToArray();
            StackLength = StackDeltas.Sum();
        }

        int GetObjectVariableStackCount(IVariable objectVariable)
        {
            // Create a default instance and get the type of the variable.
            var inst = objectVariable.GetDefaultInstance();
            var originalType = inst.CodeType.GetCodeType(_deltinScript);

            // The number of object variables assigned to this variable.
            int stackDelta = 0;

            // Extract every anonymous type that the type or it's type args use.
            var anonymousTypes = originalType.ExtractAnonymousTypes();

            // Get each potential variable type for T
            if (anonymousTypes.Length == 0)
                // Use the default stackDelta.
                stackDelta = originalType.GetGettableAssigner(objectVariable).StackDelta();
            else
                foreach (var anonymous in anonymousTypes)
                {
                    int typeArgIndex = Provider.TypeArgIndexFromAnonymousType(anonymous);

                    foreach (var type in Info.TypeArgTracker[typeArgIndex].UsedTypes)
                    {
                        // Get the assigner from the type.
                        var assigner = type.GetGettableAssigner(objectVariable);

                        /* Get the stackDelta of the object variable.
                        * Example:
                        class Axe<T> {
                            T myValue;
                        }
                        struct Bash {
                            String myValue;
                            Number myOtherValue;
                        }
                        * If Axe is used with these generics 'Axe<String>' and 'Axe<Bash>'
                        * 'Axe' will need 2 object variables assigned to it.
                        * 'String' requires 1, while 'Bash' requires 2.
                        * The higher value is used. */
                        stackDelta = Math.Max(stackDelta, assigner.StackDelta());

                        /*
                        * TODO: This is not compatible in this scenario:
                        class Axe<T> { Bash<T> myBash; }
                        struct Bash<T> { T myValue; }
                        struct Fooly { String value1; String value2; }
                        
                        Axe<Fooly> axeFooly;
                        * StackDelta may need the ProviderTrackerInfo
                        * Or we can just not support structs w/ generics, because I want to live my life I guess.
                        */
                    }
                }

            return stackDelta;
        }

        public void AddVariableInstancesToAssigner(IVariableInstance[] instances, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            for (int i = 0; i < instances.Length; i++)
            {
                int stack = Offset;
                for (int s = 0; s < i; s++)
                    stack += StackDeltas[i];

                assigner.Add(
                    instances[i].Provider,
                    instances[i].GetAssigner(null).AssignClassStacks(new GetClassStacks(_deltinScript, stack)).ChildFromClassReference(reference)
                );
            }
        }
    }
}