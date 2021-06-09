using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public interface ISelfContainedClass
    {
        string Name { get; }
        MarkupBuilder Documentation { get; }
        SelfContainedClassInstance Instance { get;}
        void Setup(SetupSelfContainedClass setup);
        void New(ActionSet actionSet, NewClassInfo newClassInfo);
    }

    public class SetupSelfContainedClass
    {
        public SelfContainedClassInstance WorkingInstance { get; }
        public Scope ObjectScope { get; }
        public Scope StaticScope { get; }
        public IEnumerable<ObjectVariable> ObjectVariables => _objectVariables;
        public IEnumerable<Constructor> Constructors => _constructors;

        readonly List<ObjectVariable> _objectVariables = new List<ObjectVariable>();
        readonly List<Constructor> _constructors = new List<Constructor>(); 

        public SetupSelfContainedClass(SelfContainedClassInstance workingInstance, Scope objectScope, Scope staticScope)
        {
            WorkingInstance = workingInstance;
            ObjectScope = objectScope;
            StaticScope = staticScope;
        }

        /// <summary>Creates an ObjectVariable and adds it to the object scope.</summary>
        public ObjectVariable AddObjectVariable(IVariableInstance variableInstance)
        {
            // Add the variable to the object scope.
            ObjectScope.AddNativeVariable(variableInstance);

            // Create the ObjectVariable.
            var result = new ObjectVariable(WorkingInstance, variableInstance);

            // Add it to the list.
            _objectVariables.Add(result);
            return result;
        }

        public void AddConstructor(Constructor constructor) => _constructors.Add(constructor);
    }

    public class SelfContainedClassProvider : ClassInitializer
    {
        public SelfContainedClassInstance Instance { get; }
        readonly ISelfContainedClass _selfContainedClass;
        readonly DeltinScript _deltinScript;

        public SelfContainedClassProvider(DeltinScript deltinScript, ISelfContainedClass selfContainedClass) : base(selfContainedClass.Name)
        {
            _selfContainedClass = selfContainedClass;
            _deltinScript = deltinScript;
            GenericTypes = new AnonymousType[0];

            Instance = new SelfContainedClassInstance(deltinScript, selfContainedClass, this);
        }

        public override CodeType GetInstance() => Instance;
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => Instance;
    }

    public class SelfContainedClassInstance : ClassType, IGetMeta
    {
        readonly ISelfContainedClass _classInfo;

        public SelfContainedClassInstance(DeltinScript deltinScript, ISelfContainedClass classInfo, SelfContainedClassProvider provider) : base(classInfo.Name, provider)
        {
            _classInfo = classInfo;
            ObjectScope = new Scope();
            StaticScope = new Scope();
            deltinScript.StagedInitiation.On(this);
        }

        public void GetMeta()
        {
            var setup = new SetupSelfContainedClass(this, ObjectScope, StaticScope);
            _classInfo.Setup(setup);

            Variables = setup.ObjectVariables.Select(ov => ov.Variable).ToArray();
            Constructors = setup.Constructors.ToArray();
        }

        protected override void New(ActionSet actionSet, NewClassInfo classInfo)
            => _classInfo.New(actionSet, classInfo);
    }
}