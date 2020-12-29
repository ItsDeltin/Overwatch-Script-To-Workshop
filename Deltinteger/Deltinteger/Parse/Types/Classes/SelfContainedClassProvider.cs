namespace Deltin.Deltinteger.Parse
{
    public abstract class SelfContainedClassProvider : ClassInitializer
    {
        /// <summary>Used in static methods and returned when ReturningScope() is called. Contains all static members in the inheritance tree.</summary>
        public Scope StaticScope { get; protected set; }

        /// <summary>Contains all object members in the inheritance tree. Returned when GetObjectScope() is called.</summary>
        public Scope ObjectScope { get; protected set; }

        protected SelfContainedClassProvider(string name) : base(name)
        {
            StaticScope = new Scope(name);
            ObjectScope = new Scope(name);
        }

        protected ObjectVariable AddObjectVariable(IVariableInstance variable)
        {
            ObjectScope.AddNativeVariable(variable);
            return base.AddObjectVariable(variable.Provider);
        }

        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new ClassType(this) {
            StaticScope = StaticScope,
            ServeObjectScope = ObjectScope
        };
    }
}