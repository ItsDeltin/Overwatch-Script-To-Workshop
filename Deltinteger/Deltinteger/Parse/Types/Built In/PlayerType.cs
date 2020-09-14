using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class PlayerType : CodeType, IAdditionalArray
    {
        // These functions are shared with both Player and Player[] types.
        // * Teleport *
        public static readonly FuncMethod Teleport = new FuncMethodBuilder() {
            Name = "Teleport",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to teleport the player or players to. Can be a player or a vector.", Positionable.Instance)
            },
            Documentation = "Teleports one or more players to the specified location.",
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Element.Part("Teleport", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };
        // * SetMoveSpeed *
        public static readonly FuncMethod SetMoveSpeed = new FuncMethodBuilder() {
            Name = "SetMoveSpeed",
            Parameters = new CodeParameter[] {
                new CodeParameter("moveSpeedPercent", "The percentage of raw move speed to which the player or players will set their move speed.", NumberType.Instance)
            },
            Documentation = "Sets the move speed of one or more players.",
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Element.Part("Set Move Speed", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };
        // * SetMaxHealth *
        public static readonly FuncMethod SetMaxHealth = new FuncMethodBuilder() {
            Name = "SetMaxHealth",
            Parameters = new CodeParameter[] {
                new CodeParameter("healthPercent", "The percentage of raw max health to which the player or players will set their max health.", NumberType.Instance)
            },
            Documentation = "Sets the move speed of one or more players.",
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Element.Part("Set Max Health", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };

        public static readonly PlayerType Instance = new PlayerType();
        public static readonly CodeType PlayerOrPlayers = new PipeType(Instance, new ArrayType(Instance));

        private readonly Scope ObjectScope = new Scope();
        private VariableShorthand[] Variables { get; } = new VariableShorthand[] {
            new VariableShorthand("Team", TeamType.Instance, r => Element.Part("Team Of", r)),
            new VariableShorthand("Position", VectorType.Instance, r => Element.PositionOf(r)),
            new VariableShorthand("Health", NumberType.Instance, r => Element.Part("Health", r)),
            new VariableShorthand("MaxHealth", NumberType.Instance, r => Element.Part("Max Health", r)),
            new VariableShorthand("FacingDirection", VectorType.Instance, r => Element.Part("Facing Direction Of", r)),
            new VariableShorthand("Hero", ObjectType.Instance, r => Element.Part("Hero Of", r)), // TODO: Hero type
            new VariableShorthand("IsHost", BooleanType.Instance, r => Element.Compare(r, Operator.Equal, Element.Part("Host Player"))),
            new VariableShorthand("IsAlive", BooleanType.Instance, r => Element.Part("IsAlive", r)),
            new VariableShorthand("IsDead", BooleanType.Instance, r => Element.Part("IsDead", r)),
            new VariableShorthand("IsCrouching", BooleanType.Instance, r => Element.Part("IsCrouching", r)),
            new VariableShorthand("IsDummy", BooleanType.Instance, r => Element.Part("IsDummyBot", r)),
            new VariableShorthand("IsFiringPrimary", BooleanType.Instance, r => Element.Part("IsFiringPrimary", r)),
            new VariableShorthand("IsFiringSecondary", BooleanType.Instance, r => Element.Part("IsFiringSecondary", r)),
            new VariableShorthand("IsInAir", BooleanType.Instance, r => Element.Part("IsInAir", r)),
            new VariableShorthand("IsOnGround", BooleanType.Instance, r => Element.Part("IsOnGround", r)),
            new VariableShorthand("IsInSpawnRoom", BooleanType.Instance, r => Element.Part("IsInSpawnRoom", r)),
            new VariableShorthand("IsMoving", BooleanType.Instance, r => Element.Part("IsMoving", r)),
            new VariableShorthand("IsOnObjective", BooleanType.Instance, r => Element.Part("IsOnObjective", r)),
            new VariableShorthand("IsOnWall", BooleanType.Instance, r => Element.Part("IsOnWall", r)),
            new VariableShorthand("IsPortraitOnFire", BooleanType.Instance, r => Element.Part("IsPortraitOnFire", r)),
            new VariableShorthand("IsStanding", BooleanType.Instance, r => Element.Part("IsStanding", r)),
            new VariableShorthand("IsUsingAbility1", BooleanType.Instance, r => Element.Part("IsUsingAbility1", r)),
            new VariableShorthand("IsUsingAbility2", BooleanType.Instance, r => Element.Part("IsUsingAbility2", r)),
            new VariableShorthand("IsUsingUltimate", BooleanType.Instance, r => Element.Part("IsUsingUltimate", r)),
        };

        private PlayerType() : base("Player")
        {
            CanBeExtended = false;
            Inherit(Positionable.Instance, null, null);
            Kind = "struct";
        }

        public void ResolveElements()
        {
            foreach (VariableShorthand shorthand in Variables)
                ObjectScope.AddNativeVariable(shorthand.Variable);
            
            PlayerType.AddSharedFunctionsToScope(ObjectScope);

            AddFunc(new FuncMethodBuilder() {
                Name = "IsButtonHeld",
                Parameters = new CodeParameter[] { new CodeParameter("button", ValueGroupType.GetEnumType("Button")) },
                ReturnType = BooleanType.Instance,
                Action = (set, call) => Element.Part("IsButtonHeld", set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is holding a button."
            });
            AddFunc(new FuncMethodBuilder() {
                Name = "IsCommunicating",
                Parameters = new CodeParameter[] { new CodeParameter("communication", ValueGroupType.GetEnumType("Communication")) },
                ReturnType = BooleanType.Instance,
                Action = (set, call) => Element.Part("IsCommunicating", set.CurrentObject, call.ParameterValues[0]),
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
        private void AddFunc(FuncMethodBuilder builder) => ObjectScope.AddNativeMethod(new FuncMethod(builder));
        public void OverrideArray(ArrayType array) => AddSharedFunctionsToScope(array.Scope);
        public static void AddSharedFunctionsToScope(Scope scope)
        {
            scope.AddNativeMethod(Teleport);
            scope.AddNativeMethod(SetMoveSpeed);
            scope.AddNativeMethod(SetMaxHealth);
        }
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