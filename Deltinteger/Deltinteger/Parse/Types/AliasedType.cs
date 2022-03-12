using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    class AliasedType : CodeType
    {
        readonly CodeType _aliasing;

        public AliasedType(string name, CodeType aliasing) : base(name)
        {
            _aliasing = aliasing;
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo) => _aliasing.GetRealType(instanceInfo);
        public override bool CompatibleWith(CodeType type) => _aliasing.CompatibleWith(type);
        public override bool Is(CodeType type) => _aliasing.Is(type);
        public override bool Implements(CodeType type) => _aliasing.Implements(type);
        public override CodeType[] UnionTypes() => _aliasing.UnionTypes();
        public override Scope ReturningScope() => _aliasing.ReturningScope();
        public override Scope GetObjectScope() => _aliasing.GetObjectScope();
        public override bool IsConstant() => _aliasing.IsConstant();
        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, WorkshopParameter[] parameters) => _aliasing.New(actionSet, constructor, parameters);
        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner) => _aliasing.AddObjectVariablesToAssigner(toWorkshop, reference, assigner);
        public override void Delete(ActionSet actionSet, Element reference) => _aliasing.Delete(actionSet, reference);
    }
}