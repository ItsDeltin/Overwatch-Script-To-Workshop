using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class PlayerType : CodeType, ITypeArrayHandler, IGetMeta
    {
        // These functions are shared with both Player and Player[] types.
        // * Teleport *
        FuncMethod Teleport => new FuncMethodBuilder()
        {
            Name = "Teleport",
            Parameters = new CodeParameter[] {
                new CodeParameter("position", "The position to teleport the player or players to. Can be a player or a vector.", _supplier.PlayerOrVector())
            },
            Documentation = "Teleports one or more players to the specified location.",
            Action = (actionSet, methodCall) =>
            {
                actionSet.AddAction(Element.Part("Teleport", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };
        // * SetMoveSpeed *
        FuncMethod SetMoveSpeed => new FuncMethodBuilder()
        {
            Name = "SetMoveSpeed",
            Parameters = new CodeParameter[] {
                new CodeParameter("moveSpeedPercent", "The percentage of raw move speed to which the player or players will set their move speed.", _supplier.Number())
            },
            Documentation = "Sets the move speed of one or more players.",
            Action = (actionSet, methodCall) =>
            {
                actionSet.AddAction(Element.Part("Set Move Speed", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };
        // * SetMaxHealth *
        FuncMethod SetMaxHealth => new FuncMethodBuilder()
        {
            Name = "SetMaxHealth",
            Parameters = new CodeParameter[] {
                new CodeParameter("healthPercent", "The percentage of raw max health to which the player or players will set their max health.", _supplier.Number())
            },
            Documentation = "Sets the move speed of one or more players.",
            Action = (actionSet, methodCall) =>
            {
                actionSet.AddAction(Element.Part("Set Max Health", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };
        // * AllowButton *
        FuncMethod AllowButton => new FuncMethodBuilder()
        {
            Name = "AllowButton",
            Parameters = new CodeParameter[] {
                new CodeParameter("button", "The logical button that is being reenabled.", _supplier.Button())
            },
            Documentation = "Undoes the effect of the disallow button action for one or more players.",
            Action = (actionSet, methodCall) =>
            {
                actionSet.AddAction(Element.Part("Allow Button", actionSet.CurrentObject, methodCall.ParameterValues[0]));
                return null;
            }
        };

        public Scope PlayerVariableScope { get; } = new Scope("player variables") { TagPlayerVariables = true };
        readonly ITypeSupplier _supplier;
        Scope _objectScope;

        public PlayerType(DeltinScript deltinScript, ITypeSupplier typeSupplier) : base("Player")
        {
            AsReferenceResetSettability = true;
            ArrayHandler = this;
            _supplier = typeSupplier;

            deltinScript.StagedInitiation.On(this);
        }

        public void GetMeta()
        {
            _objectScope = PlayerVariableScope.Child();
            AddSharedFunctionsToScope(_objectScope);

            AddFunc(new FuncMethodBuilder()
            {
                Name = "IsButtonHeld",
                Parameters = new[] { new CodeParameter("button", _supplier.EnumType("Button")) },
                ReturnType = _supplier.Boolean(),
                Action = (set, call) => Element.Part("Is Button Held", set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is holding a button."
            });
            AddFunc(new FuncMethodBuilder()
            {
                Name = "IsCommunicating",
                Parameters = new[] { new CodeParameter("communication", _supplier.EnumType("Communication")) },
                ReturnType = _supplier.Boolean(),
                Action = (set, call) => Element.Part("Is Communicating", set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Determines if the target player is communicating."
            });
            AddFunc(new FuncMethodBuilder()
            {
                Name = "Stat",
                Parameters = new[] { new CodeParameter("stat", _supplier.EnumType("PlayerStat")) },
                ReturnType = _supplier.Number(),
                Action = (set, call) => Element.Part("Player Stat", set.CurrentObject, call.ParameterValues[0]),
                Documentation = "Provides a statistic of the specified Player (limited to the current match). Statistics are only gathered when the game is in progress. Dummy bots do not gather statistics.",
            });
            AddFunc(new FuncMethodBuilder()
            {
                Name = "HeroStat",
                Parameters = new[] { new CodeParameter("hero", _supplier.Hero()), new CodeParameter("stat", _supplier.EnumType("PlayerHeroStat")) },
                ReturnType = _supplier.Number(),
                Action = (set, call) => Element.Part("Player Hero Stat", set.CurrentObject, call.ParameterValues[0], call.ParameterValues[1]),
                Documentation = "Provides a statistic of the specified Player's time playing a specific hero (limited to the current match). Statistics are only gathered when the game is in progress. Dummy bots do not gather statistics.",
            });
            AddFunc(new FuncMethodBuilder()
            {
                Name = "DistanceTo",
                Parameters = new[] { new CodeParameter("position", _supplier.PlayerOrVector()) },
                ReturnType = _supplier.Number(),
                Action = (set, call) => Element.DistanceBetween(set.CurrentObject, call.ParameterValues[0]),
                Documentation = "The distance between the player and the specified player or vector",
            });
            AddFunc("Position", _supplier.Vector(), set => Element.PositionOf(set.CurrentObject), "The position of the player.");
            AddFunc("EyePosition", _supplier.Vector(), set => Element.EyePosition(set.CurrentObject), "The position of the player's head.");
            AddFunc("Team", _supplier.Any(), set => Element.Part("Team Of", set.CurrentObject), "The team of the player.");
            AddFunc("Health", _supplier.Number(), set => Element.Part("Health", set.CurrentObject), "The health of the player.");
            AddFunc("MaxHealth", _supplier.Number(), set => Element.Part("Max Health", set.CurrentObject), "The maximum health of the player.");
            AddFunc("FacingDirection", _supplier.Vector(), set => Element.FacingDirectionOf(set.CurrentObject), "The facing direction of the player.");
            AddFunc("Hero", _supplier.Any(), set => Element.Part("Hero Of", set.CurrentObject), "The hero of the player.");
            AddFunc("IsHost", _supplier.Boolean(), set => Element.Compare(set.CurrentObject, Operator.Equal, Element.Part("Host Player")), "Determines if the player is the host.");
            AddFunc("IsAlive", _supplier.Boolean(), set => Element.Part("Is Alive", set.CurrentObject), "Determines if the player is alive.");
            AddFunc("IsDead", _supplier.Boolean(), set => Element.Part("Is Dead", set.CurrentObject), "Determines if the player is dead.");
            AddFunc("IsCrouching", _supplier.Boolean(), set => Element.Part("Is Crouching", set.CurrentObject), "Determines if the player is crouching.");
            AddFunc("IsDummy", _supplier.Boolean(), set => Element.Part("Is Dummy Bot", set.CurrentObject), "Determines if the player is a dummy bot.");
            AddFunc("IsFiringPrimary", _supplier.Boolean(), set => Element.Part("Is Firing Primary", set.CurrentObject), "Determines if the player is firing their primary weapon.");
            AddFunc("IsFiringSecondary", _supplier.Boolean(), set => Element.Part("Is Firing Secondary", set.CurrentObject), "Determines if the player is using their secondary attack.");
            AddFunc("IsInAir", _supplier.Boolean(), set => Element.Part("Is In Air", set.CurrentObject), "Determines if the player is in the air.");
            AddFunc("IsOnGround", _supplier.Boolean(), set => Element.Part("Is On Ground", set.CurrentObject), "Determines if the player is on the ground.");
            AddFunc("IsInSpawnRoom", _supplier.Boolean(), set => Element.Part("Is In Spawn Room", set.CurrentObject), "Determines if the player is in the spawn room.");
            AddFunc("IsMoving", _supplier.Boolean(), set => Element.Part("Is Moving", set.CurrentObject), "Determines if the player is moving.");
            AddFunc("IsOnObjective", _supplier.Boolean(), set => Element.Part("Is On Objective", set.CurrentObject), "Determines if the player is on the objective.");
            AddFunc("IsOnWall", _supplier.Boolean(), set => Element.Part("Is On Wall", set.CurrentObject), "Determines if the player is on a wall.");
            AddFunc("IsPortraitOnFire", _supplier.Boolean(), set => Element.Part("Is Portrait On Fire", set.CurrentObject), "Determines if the player's portrait is on fire.");
            AddFunc("IsStanding", _supplier.Boolean(), set => Element.Part("Is Standing", set.CurrentObject), "Determines if the player is standing.");
            AddFunc("IsUsingAbility1", _supplier.Boolean(), set => Element.Part("Is Using Ability 1", set.CurrentObject), "Determines if the player is using their ability 1.");
            AddFunc("IsUsingAbility2", _supplier.Boolean(), set => Element.Part("Is Using Ability 2", set.CurrentObject), "Determines if the player is using their ability 2.");
        }

        public override CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope GetObjectScope() => _objectScope;
        public override Scope ReturningScope() => null;
        public void OverrideArray(ArrayType array)
        {
            AddSharedFunctionsToScope(array.Scope);
            array.Scope.TagPlayerVariables = true;
            array.Scope.CopyAll(PlayerVariableScope);
        }
        void AddSharedFunctionsToScope(Scope scope)
        {
            scope.AddNativeMethod(Teleport);
            scope.AddNativeMethod(SetMoveSpeed);
            scope.AddNativeMethod(SetMaxHealth);
            scope.AddNativeMethod(AllowButton);
            scope.AddNativeMethod(SetAbilityEnabled("Ability 1"));
            scope.AddNativeMethod(SetAbilityEnabled("Ability 2"));
            scope.AddNativeMethod(SetAbilityEnabled("Primary Fire"));
            scope.AddNativeMethod(SetAbilityEnabled("Secondary Fire"));
            scope.AddNativeMethod(SetAbilityEnabled("Ultimate Ability"));
            scope.AddNativeMethod(SetAbilityEnabled("Crouch"));
            scope.AddNativeMethod(SetAbilityEnabled("Melee"));
            scope.AddNativeMethod(SetAbilityEnabled("Jump"));
            scope.AddNativeMethod(SetAbilityEnabled("Reload"));
        }

        private void AddFunc(FuncMethodBuilder builder) => _objectScope.AddNativeMethod(new FuncMethod(builder));
        private void AddFunc(string name, CodeType returnType, Func<ActionSet, IWorkshopTree> action, string documentation)
            => AddFunc(new FuncMethodBuilder()
            {
                Name = name,
                ReturnType = returnType,
                Action = (actionSet, methodCall) => action(actionSet),
                Documentation = documentation
            });

        private FuncMethod SetAbilityEnabled(string abilityName) => new FuncMethodBuilder()
        {
            Name = "Set" + abilityName.Replace(" ", "") + "Enabled",
            Documentation = $"Enables or disables the {abilityName.ToLower()} for one or more players.",
            Parameters = new[] { new CodeParameter("enabled", $"Specifies whether the player or players are able to use their {abilityName.ToLower()}.", _supplier.Boolean()) },
            Action = (actionSet, methodCall) => Element.Part("Set " + abilityName + " Enabled", actionSet.CurrentObject, methodCall.Get(0))
        };

        IGettableAssigner ITypeArrayHandler.GetArrayAssigner(AssigningAttributes attributes) => null;
        public ArrayFunctionHandler GetFunctionHandler() => new ArrayFunctionHandler();
    }
}