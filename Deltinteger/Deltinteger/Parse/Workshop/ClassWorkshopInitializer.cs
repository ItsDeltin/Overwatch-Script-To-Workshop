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
        int _newClassID = 1; // Counts up from 0 assigning classes identifiers.
        readonly Dictionary<IClassInitializer, WorkshopInitializedClass> _initialized = new Dictionary<IClassInitializer, WorkshopInitializedClass>();

        public ClassWorkshopInitializerComponent(ToWorkshop toWorkshop)
        {
            _toWorkshop = toWorkshop;

            // Init classes then assign stacks.
            InitClasses();
            AssignStacks();
        }

        // Initializes all classes in the tracker.
        void InitClasses()
        {
            foreach (var tracker in _typeTracker.Trackers)
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
            var wic = new WorkshopInitializedClass(_toWorkshop, provider, info, stackOffset, _newClassID);
            _newClassID++;

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
                _stacks[i] = _toWorkshop.DeltinScript.VarCollection.Assign(ObjectVariableTag + i, true, false);
        }

        public WorkshopInitializedClass InitializedClassFromProvider(IClassInitializer provider) => _initialized[provider];
        public int GetIdentifier(IClassInitializer provider) => _initialized[provider].ID;
        public IndexReference ObjectVariableFromIndex(int i) => _stacks[i];
    }

    public class WorkshopInitializedClass
    {
        readonly ToWorkshop _toWorkshop;
        public IClassInitializer Provider { get; }
        public ProviderTrackerInfo Info { get; }
        public int Offset { get; }
        public int[] StackDeltas { get; }
        public int StackLength { get; }
        public int ID { get; }

        public WorkshopInitializedClass(
            ToWorkshop toWorkshop,
            IClassInitializer provider,
            ProviderTrackerInfo info,
            int stackOffset,
            int id
        )
        {
            _toWorkshop = toWorkshop;
            Provider = provider;
            Info = info;
            Offset = stackOffset;
            ID = id;

            StackDeltas = provider.ObjectVariables.Select(ov => GetObjectVariableStackCount(ov)).ToArray();
            StackLength = StackDeltas.Sum();
        }

        int GetObjectVariableStackCount(IVariable objectVariable)
        {
            // Create a default instance and get the type of the variable.
            var inst = objectVariable.GetDefaultInstance();
            var originalType = inst.CodeType.GetCodeType(_toWorkshop.DeltinScript);

            // The number of object variables assigned to this variable.
            int stackDelta = 0;

            // Extract every anonymous type that the type or it's type args use.
            var anonymousTypes = originalType.ExtractAnonymousTypes();

            // Get each potential variable type for T
            if (anonymousTypes.Length == 0)
                // Use the default stackDelta.
                stackDelta = originalType.GetGettableAssigner(AssigningAttributes.Empty).StackDelta();
            else
                foreach (var anonymous in anonymousTypes)
                {
                    int typeArgIndex = Provider.TypeArgIndexFromAnonymousType(anonymous);

                    foreach (var type in Info.TypeArgs[typeArgIndex].AllTypeArguments)
                    {
                        // Get the assigner from the type.
                        var assigner = type.GetGettableAssigner(AssigningAttributes.Empty);

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
                        TODO: This is not compatible in this scenario:
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
                    instances[i].GetAssigner(null).AssignClassStacks(new GetClassStacks(_toWorkshop.ClassInitializer, stack)).ChildFromClassReference(reference)
                );
            }
        }
    }
}