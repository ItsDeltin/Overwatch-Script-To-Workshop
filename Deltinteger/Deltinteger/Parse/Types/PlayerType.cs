using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class PlayerType : CodeType
    {
        public static readonly PlayerType Instance = new PlayerType();
        private readonly Scope ObjectScope = new Scope();
        private VariableShorthand[] Variables { get; } = new VariableShorthand[] {
            new VariableShorthand("Team", TeamType.Instance, r => Element.Part<V_TeamOf>(r)),
            new VariableShorthand("Position", VectorType.Instance, r => Element.Part<V_PositionOf>(r)),
            new VariableShorthand("Health", NumberType.Instance, r => Element.Part<V_Health>(r)),
            new VariableShorthand("MaxHealth", NumberType.Instance, r => Element.Part<V_MaxHealth>(r)),
            new VariableShorthand("FacingDirection", VectorType.Instance, r => Element.Part<V_FacingDirectionOf>(r)),
            new VariableShorthand("Hero", ObjectType.Instance, r => Element.Part<V_HeroOf>(r)), // TODO: Hero type
            new VariableShorthand("IsHost", BooleanType.Instance, r => new V_Compare(r, Operators.Equal, new V_HostPlayer())),
            new VariableShorthand("IsAlive", BooleanType.Instance, r => Element.Part<V_IsAlive>(r)),
            new VariableShorthand("IsDead", BooleanType.Instance, r => Element.Part<V_IsDead>(r)),
            new VariableShorthand("IsCrouching", BooleanType.Instance, r => Element.Part<V_IsCrouching>(r)),
            new VariableShorthand("IsDummy", BooleanType.Instance, r => Element.Part<V_IsDummyBot>(r)),
            new VariableShorthand("IsFiringPrimary", BooleanType.Instance, r => Element.Part<V_IsFiringPrimary>(r)),
            new VariableShorthand("IsFiringSecondary", BooleanType.Instance, r => Element.Part<V_IsFiringSecondary>(r)),
            new VariableShorthand("IsInAir", BooleanType.Instance, r => Element.Part<V_IsInAir>(r)),
            new VariableShorthand("IsOnGround", BooleanType.Instance, r => Element.Part<V_IsOnGround>(r)),
            new VariableShorthand("IsInSpawnRoom", BooleanType.Instance, r => Element.Part<V_IsInSpawnRoom>(r)),
            new VariableShorthand("IsMoving", BooleanType.Instance, r => Element.Part<V_IsMoving>(r)),
            new VariableShorthand("IsOnObjective", BooleanType.Instance, r => Element.Part<V_IsOnObjective>(r)),
            new VariableShorthand("IsOnWall", BooleanType.Instance, r => Element.Part<V_IsOnWall>(r)),
            new VariableShorthand("IsPortraitOnFire", BooleanType.Instance, r => Element.Part<V_IsPortraitOnFire>(r)),
            new VariableShorthand("IsStanding", BooleanType.Instance, r => Element.Part<V_IsStanding>(r)),
            new VariableShorthand("IsUsingAbility1", BooleanType.Instance, r => Element.Part<V_IsUsingAbility1>(r)),
            new VariableShorthand("IsUsingAbility2", BooleanType.Instance, r => Element.Part<V_IsUsingAbility2>(r)),
            new VariableShorthand("IsUsingUltimate", BooleanType.Instance, r => Element.Part<V_IsUsingUltimate>(r)),
        };

        private PlayerType() : base("Player")
        {
            CanBeExtended = false;
            Inherit(ObjectType.Instance, null, null);

            foreach (VariableShorthand shorthand in Variables)
                ObjectScope.AddNativeVariable(shorthand.Variable);
            
            AddFunc("IsButtonHeld", new CodeParameter[] { new CodeParameter("button", WorkshopEnumType.GetEnumType<Button>()) }, (set, call) => Element.Part<V_IsButtonHeld>(set.CurrentObject, call.ParameterValues[0]));
            AddFunc("IsCommunicating", new CodeParameter[] { new CodeParameter("communication", WorkshopEnumType.GetEnumType<Communication>()) }, (set, call) => Element.Part<V_IsCommunicating>(set.CurrentObject, call.ParameterValues[0]));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            foreach (VariableShorthand shorthand in Variables)
                shorthand.Assign(reference, assigner);
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => null;

        private void AddFunc(string name, CodeParameter[] parameters, Func<ActionSet, MethodCall, IWorkshopTree> action)
        {
            ObjectScope.AddNativeMethod(new FuncMethod(name, parameters, action));
        }

        class VariableShorthand
        {
            public InternalVar Variable { get; }
            private Func<IWorkshopTree, IWorkshopTree> Reference { get; }

            public VariableShorthand(string name, CodeType type, Func<IWorkshopTree, IWorkshopTree> reference)
            {
                Variable = new InternalVar(name, CompletionItemKind.Property) {
                    CodeType = type,
                    VariableType = VariableType.ElementReference
                };
                Reference = reference;
            }

            public void Assign(IWorkshopTree reference, VarIndexAssigner assigner)
            {
                assigner.Add(Variable, Reference.Invoke(reference));
            }
        }
    }
}