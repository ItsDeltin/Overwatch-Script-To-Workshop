namespace Deltin.Deltinteger.Parse
{
    public abstract class SelfContainedClassProvider : ClassInitializer
    {
        /// <summary>Used in static methods and returned when ReturningScope() is called. Contains all static members in the inheritance tree.</summary>
        public Scope StaticScope { get; protected set; }

        /// <summary>Contains all object members in the inheritance tree. Returned when GetObjectScope() is called.</summary>
        public Scope ObjectScope { get; protected set; }

        public bool DidWorkshopInit { get; set; }

        protected SelfContainedClassProvider(string name) : base(name)
        {
            StaticScope = new Scope(name);
            ObjectScope = new Scope(name);
        }

        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new SelfContainedClassInstance(this) {
            StaticScope = StaticScope,
            ServeObjectScope = ObjectScope
        };

        public abstract void WorkshopInit(DeltinScript translateInfo);
        public abstract void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner);
        public abstract void New(ActionSet actionSet, NewClassInfo newClassInfo);
    }

    public class SelfContainedClassInstance : ClassType
    {
        private readonly SelfContainedClassProvider _provider;

        public SelfContainedClassInstance(SelfContainedClassProvider initializer) : base(initializer)
        {
            _provider = initializer;
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            if (_provider.DidWorkshopInit) return;
            _provider.DidWorkshopInit = true;
            base.WorkshopInit(translateInfo);
            _provider.WorkshopInit(translateInfo);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
            => _provider.AddObjectVariablesToAssigner(reference, assigner);

        protected override void New(ActionSet actionSet, NewClassInfo classInfo)
            => _provider.New(actionSet, classInfo);
    }
}