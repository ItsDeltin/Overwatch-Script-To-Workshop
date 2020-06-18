using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class PlayerType : CodeType, IAdditionalArray
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
            Inherit(Positionable.Instance, null, null);

            foreach (VariableShorthand shorthand in Variables)
                ObjectScope.AddNativeVariable(shorthand.Variable);
            
            AddFunc(new FuncMethodBuilder() {
                Name = "IsButtonHeld",
                Parameters = new CodeParameter[] { new CodeParameter("button", ValueGroupType.GetEnumType<Button>()) },
                ReturnType = BooleanType.Instance,
                Action = (set, call) => Element.Part<V_IsButtonHeld>(set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is holding a button."
            });
            AddFunc(new FuncMethodBuilder() {
                Name = "IsCommunicating",
                Parameters = new CodeParameter[] { new CodeParameter("communication", ValueGroupType.GetEnumType<Communication>()) },
                ReturnType = BooleanType.Instance,
                Action = (set, call) => Element.Part<V_IsCommunicating>(set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is communicating."
            });
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

        private void AddFunc(FuncMethodBuilder builder)
        {
            ObjectScope.AddNativeMethod(new FuncMethod(builder));
        }

        public void OverrideArray(ArrayType array)
        {
            foreach (var function in PlayersType.SharedFunctions)
                array.Scope.AddNativeMethod(function);
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

    /// <summary>The players type is either a Player or Player[].</summary>
    public class PlayersType : CodeType
    {
        public static readonly PlayersType Instance = new PlayersType();
        private readonly Scope ObjectScope = new Scope();

        private PlayersType() : base("Players") {}

        public void ResolveElements()
        {
            ObjectScope.AddNativeMethod(Teleport);
            SharedFunctions = new FuncMethod[] {
                Teleport
            };
        }

        public override bool Implements(CodeType type) => type.Implements(PlayerType.Instance) || new ArrayType(type).Implements(new ArrayType(PlayerType.Instance));

        public override Scope ReturningScope() => null;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };

        public static FuncMethod[] SharedFunctions;

        // These functions are shared with both all Player, Player[], and Players type.
        public static FuncMethod Teleport { get; } = new FuncMethodBuilder() {
            Name = "Teleport",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to teleport the player or players to. Can be a player or a vector.", Positionable.Instance)
            },
            Documentation = "Teleports one or more players to the specified location.",
            Action = (actionSet, methodCall) => Element.Part<A_Teleport>(actionSet.CurrentObject, methodCall.ParameterValues[0])
        };
    }
}