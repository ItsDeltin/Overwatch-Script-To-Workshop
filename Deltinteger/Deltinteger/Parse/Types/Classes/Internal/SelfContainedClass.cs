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
        Constructor[] Constructors { get; }
        SelfContainedClassInstance Instance { get;}
        void Setup(ISetupSelfContainedClass setup);
        void WorkshopInit(DeltinScript deltinScript);
        void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner);
        void New(ActionSet actionSet, NewClassInfo newClassInfo);
    }

    public interface ISetupSelfContainedClass
    {
        CodeType WorkingInstance { get; }
        Scope ObjectScope { get; }
        Scope StaticScope { get; }
        void AddObjectVariable(IVariableInstance variableInstance);
    }

    class SetupSelfContainedClass : ISetupSelfContainedClass
    {
        public CodeType WorkingInstance { get; }
        public Scope ObjectScope { get; }
        public Scope StaticScope { get; }
        public IReadOnlyList<IVariableInstance> ObjectVariables => _objectVariables;
        private readonly List<IVariableInstance> _objectVariables = new List<IVariableInstance>();

        public void AddObjectVariable(IVariableInstance variableInstance)
        {
            _objectVariables.Add(variableInstance);
            ObjectScope.AddNativeVariable(variableInstance);
        }

        public SetupSelfContainedClass(CodeType workingInstance, Scope objectScope, Scope staticScope)
        {
            WorkingInstance = workingInstance;
            ObjectScope = objectScope;
            StaticScope = staticScope;
        }
    }

    public class SelfContainedClassInstance : ClassType
    {
        private readonly ISelfContainedClass _classInfo;
        private bool _didWorkshopInit;

        public SelfContainedClassInstance(ISelfContainedClass classInfo) : base(classInfo.Name)
        {
            _classInfo = classInfo;
        }

        public override void WorkshopInit(DeltinScript deltinScript)
        {
            if (_didWorkshopInit) return;
            _didWorkshopInit = true;
            base.WorkshopInit(deltinScript);
            _classInfo.WorkshopInit(deltinScript);
        }

        public override void ResolveElements()
        {
            var setup = new SetupSelfContainedClass(this, ServeObjectScope, StaticScope);
            _classInfo.Setup(setup);
            Variables = setup.ObjectVariables.ToArray();
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            base.AddObjectVariablesToAssigner(toWorkshop, reference, assigner);
            _classInfo.AddObjectVariablesToAssigner(reference, assigner);
        }

        protected override void New(ActionSet actionSet, NewClassInfo classInfo)
            => _classInfo.New(actionSet, classInfo);
    }
}