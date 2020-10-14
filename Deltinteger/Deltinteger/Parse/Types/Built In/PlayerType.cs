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

        public readonly Scope ObjectScope = new Scope("player variables") { TagPlayerVariables = true };

        public PlayerType() : base("Player")
        {
            CanBeExtended = false;
            Inherit(Positionable.Instance, null, null);
            Kind = "struct";
        }

        public void ResolveElements()
        {            
            PlayerType.AddSharedFunctionsToScope(ObjectScope);

            AddFunc(new FuncMethodBuilder() {
                Name = "IsButtonHeld",
                Parameters = new CodeParameter[] { new CodeParameter("button", ValueGroupType.GetEnumType("Button")) },
                ReturnType = BooleanType.Instance,
                Action = (set, call) => Element.Part("Is Button Held", set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is holding a button."
            });
            AddFunc(new FuncMethodBuilder() {
                Name = "IsCommunicating",
                Parameters = new CodeParameter[] { new CodeParameter("communication", ValueGroupType.GetEnumType("Communication")) },
                ReturnType = BooleanType.Instance,
                Action = (set, call) => Element.Part("Is Communicating", set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is communicating."
            });
            AddFunc("Position", VectorType.Instance, (set, call) => Element.PositionOf(set.CurrentObject), "The position of the player.");
            AddFunc("Team", ObjectType.Instance, (set, call) => Element.Part("Team Of", set.CurrentObject), "The team of the player.");
            AddFunc("Health", NumberType.Instance, (set, call) => Element.Part("Health", set.CurrentObject), "The health of the player.");
            AddFunc("MaxHealth", NumberType.Instance, (set, call) => Element.Part("Max Health", set.CurrentObject), "The maximum health of the player.");
            AddFunc("FacingDirection", VectorType.Instance, (set, call) => Element.FacingDirectionOf(set.CurrentObject), "The facing direction of the player.");
            AddFunc("Hero", ObjectType.Instance, (set, call) => Element.Part("Hero Of", set.CurrentObject), "The hero of the player.");
            AddFunc("IsHost", BooleanType.Instance, (set, call) => Element.Compare(set.CurrentObject, Operator.Equal, Element.Part("Host Player")), "Determines if the player is the host.");
            AddFunc("IsAlive", BooleanType.Instance, (set, call) => Element.Part("Is Alive", set.CurrentObject), "Determines if the player is alive.");
            AddFunc("IsDead", BooleanType.Instance, (set, call) => Element.Part("Is Dead", set.CurrentObject), "Determines if the player is dead.");
            AddFunc("IsCrouching", BooleanType.Instance, (set, call) => Element.Part("Is Crouching", set.CurrentObject), "Determines if the player is crouching.");
            AddFunc("IsDummy", BooleanType.Instance, (set, call) => Element.Part("Is Dummy Bot", set.CurrentObject), "Determines if the player is a dummy bot.");
            AddFunc("IsFiringPrimary", BooleanType.Instance, (set, call) => Element.Part("Is Firing Primary", set.CurrentObject), "Determines if the player is firing their primary weapon.");
            AddFunc("IsFiringSecondary", BooleanType.Instance, (set, call) => Element.Part("Is Firing Secondary", set.CurrentObject), "Determines if the player is using their secondary attack.");
            AddFunc("IsInAir", BooleanType.Instance, (set, call) => Element.Part("Is In Air", set.CurrentObject), "Determines if the player is in the air.");
            AddFunc("IsOnGround", BooleanType.Instance, (set, call) => Element.Part("Is On Ground", set.CurrentObject), "Determines if the player is on the ground.");
            AddFunc("IsInSpawnRoom", BooleanType.Instance, (set, call) => Element.Part("Is In Spawn Room", set.CurrentObject), "Determines if the player is in the spawn room.");
            AddFunc("IsMoving", BooleanType.Instance, (set, call) => Element.Part("Is Moving", set.CurrentObject), "Determines if the player is moving.");
            AddFunc("IsOnObjective", BooleanType.Instance, (set, call) => Element.Part("Is On Objective", set.CurrentObject), "Determines if the player is on the objective.");
            AddFunc("IsOnWall", BooleanType.Instance, (set, call) => Element.Part("Is On Wall", set.CurrentObject), "Determines if the player is on a wall.");
            AddFunc("IsPortraitOnFire", BooleanType.Instance, (set, call) => Element.Part("Is Portrait On Fire", set.CurrentObject), "Determines if the player's portrait is on fire.");
            AddFunc("IsStanding", BooleanType.Instance, (set, call) => Element.Part("Is Standing", set.CurrentObject), "Determines if the player is standing.");
            AddFunc("IsUsingAbility1", BooleanType.Instance, (set, call) => Element.Part("Is Using Ability 1", set.CurrentObject), "Determines if the player is using their ability 1.");
            AddFunc("IsUsingAbility2", BooleanType.Instance, (set, call) => Element.Part("Is Using Ability 2", set.CurrentObject), "Determines if the player is using their ability 2.");
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => null;
        private void AddFunc(FuncMethodBuilder builder) => ObjectScope.AddNativeMethod(new FuncMethod(builder));
        private void AddFunc(string name, CodeType returnType, Func<ActionSet, MethodCall, IWorkshopTree> action, string documentation) => AddFunc(new FuncMethodBuilder() { Name = name, ReturnType = returnType, Action = action, Documentation = documentation });
        public void OverrideArray(ArrayType array)
        {
            AddSharedFunctionsToScope(array.Scope);
            array.Scope.TagPlayerVariables = true;
        }
        public static void AddSharedFunctionsToScope(Scope scope)
        {
            scope.AddNativeMethod(Teleport);
            scope.AddNativeMethod(SetMoveSpeed);
            scope.AddNativeMethod(SetMaxHealth);
        }
    }
}