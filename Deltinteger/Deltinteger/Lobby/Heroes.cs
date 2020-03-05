namespace Deltin.Deltinteger.Lobby
{
    public class HeroSettingCollection : LobbySettingCollection<HeroSettingCollection>
    {
        // ***********
        // * GLOBALS *
        // ***********
        private static readonly LobbySetting QuickMelee = new SwitchValue("Quick Melee", true);
        private static readonly LobbySetting SpawnWithUlt = new SwitchValue("Spawn With Ultimate Ready", false);
        private static readonly LobbySetting DamageDealt = new RangeValue("Damage Dealt", 10, 500);
        private static readonly LobbySetting DamageReceived = new RangeValue("Damage Received", 10, 500);
        private static readonly LobbySetting HealingDealt = new RangeValue("Healing Dealt", 10, 500);
        private static readonly LobbySetting HealingReceived = new RangeValue("Healing Received", 10, 500);
        private static readonly LobbySetting JumpVerticalSpeed = new RangeValue("Jump Vertical Speed", 25, 800);
        private static readonly LobbySetting MovementGravity = new RangeValue("Movement Gravity", 25, 400);
        private static readonly LobbySetting MovementSpeed = new RangeValue("Movement Speed", 50, 300);
        private static readonly LobbySetting ReceiveHeadshotsOnly = new SwitchValue("Receive Headshots Only", false);
        private static readonly LobbySetting PrimaryFire = new SwitchValue("Primary Fire", true);
        // * Generic Ammunition Info (Add to global) *
        private static readonly LobbySetting AmmunitionClipSizeScalar = new RangeValue("Ammunition Clip Size Scalar", 25, 500);
        private static readonly LobbySetting NoAmmunitionRequirement = new SwitchValue("No Ammunition Requirement", false);


        // * Healers *
        private static readonly LobbySetting Health = new RangeValue("Health", 10, 500);

        // * Projectiles *
        private static readonly LobbySetting ProjectileSpeed = new RangeValue("Projectile Speed", 0, 300);
        private static readonly LobbySetting ProjectileGravity = new RangeValue("Projectile Gravity", 0, 500);

        // * Ult Duration *
        private static readonly LobbySetting UltimateDuration = new RangeValue("Ultimate Duration", 25, 500);
        private static readonly LobbySetting InfiniteDuration = new SwitchValue("Infinite Ultimate Duration", false);

        // * Scope *
        private static readonly LobbySetting NoAutomaticFire = new SwitchValue("No Automatic Fire", false);
        private static readonly LobbySetting NoScope = new SwitchValue("No Scope", false);

        // * Secondary Fire *
        private static readonly LobbySetting SecondaryFire = new SwitchValue("Secondary Fire", true);

        /// <summary>An array of all heroes + general and their settings.</summary>
        public static readonly HeroSettingCollection[] AllHeroSettings = new HeroSettingCollection[] {
            new HeroSettingCollection("General").AddUlt(null, true).AddProjectile(true).AddHealer(),
            new HeroSettingCollection("Ana").AddUlt("Nano Boost").AddProjectile(false).AddHealer().AddScope().AddAbility("Biotic Grenade").AddAbility("Sleep Dart"),
            new HeroSettingCollection("Ashe").AddUlt("B.O.B.", true).AddProjectile(true).AddScope().AddAbility("Coach Gun", hasKnockback: true, selfKnockback: true).AddAbility("Dynamite").AddRange("Dynamite Fuse Time Scalar", 1),
            new HeroSettingCollection("Baptiste").AddUlt("Amplification Matrix", true).AddProjectile(false).AddHealer().AddAbility("Immortality Field").AddAbility("Regenerative Burst").AddSecondaryFire(),
            new HeroSettingCollection("Bastion").AddUlt("Configuration: Tank", true).AddProjectile(false).AddHealer().AddAbility("Reconfigure", hasCooldown: false).AddAbility("Self-Repair", rechargeable: true),
            new HeroSettingCollection("Brigitte").AddUlt("Rally", true).AddHealer().AddAbility("Repair Pack").AddAbility("Shield Bash", hasKnockback: true).AddAbility("Whip Shot", hasKnockback: true).RemoveAmmunition(),
            new HeroSettingCollection("D.va").AddUlt("Self-Destruct", true).AddAbility("Micro Missiles").AddAbility("Boosters", hasKnockback: true).AddAbility("Defense Matrix", rechargeable: true).RemoveAmmunition(),
            new HeroSettingCollection("Doomfist").AddUlt("Meteor Strike", hasKnockback: true, hasDuration: true).AddProjectile(false).AddAbility("Rising Uppercut", hasKnockback: true).AddAbility("Rocket Punch", hasKnockback: true).AddAbility("Seismic Slam").AddRange("Ammunition Regeneration Time Scalar", 33, 500),
            new HeroSettingCollection("Genji").AddUlt("Dragonblade", hasDuration: true).AddProjectile(false).AddSecondaryFire().AddAbility("Deflect").AddAbility("Swift Strike"),
            new HeroSettingCollection("Hanzo").AddUlt("Dragonstrike").AddProjectile(true).RemoveAmmunition().AddAbility("Lunge").AddRange("Lunge Distance Scalar", 20, 300).AddAbility("Sonic Arrow").AddAbility("Storm Arrow").AddIntRange("Storm Arrows Quantity", 3, 12, 5),
            new HeroSettingCollection("Junkrat").AddUlt("Rip-Tire", hasDuration: true).AddProjectile(true).AddAbility("Concussion Mine", hasKnockback: true).AddAbility("Steel Trap").AddRange("Frag Launcher Knockback Scalar", 0, 400),
            new HeroSettingCollection("Lúcio").AddUlt("Sound Barrier").AddHealer().AddProjectile(false).AddAbility("Amp It Up").AddAbility("Crossfade", hasCooldown: false).AddAbility("Soundwave", hasKnockback: true),
            new HeroSettingCollection("Mccree").AddUlt("Deadeye").AddProjectile(false).AddSecondaryFire().AddAbility("Combat Roll").AddAbility("Flashbang")
        };


        /// <summary>The name of the hero.</summary>
        public string HeroName { get; }

        public HeroSettingCollection(string heroName)
        {
            HeroName = heroName;
            Title = $"'{HeroName}' hero settings.";
            AddGlobals();
        }
        
        public HeroSettingCollection AddGlobals()
        {
            Add(QuickMelee);
            Add(SpawnWithUlt);
            Add(DamageDealt);
            Add(DamageReceived);
            Add(HealingReceived);
            Add(JumpVerticalSpeed);
            Add(MovementGravity);
            Add(MovementSpeed);
            Add(ReceiveHeadshotsOnly);
            Add(PrimaryFire);
            Add(AmmunitionClipSizeScalar);
            Add(NoAmmunitionRequirement);
            return this;
        }

        public HeroSettingCollection AddHealer()
        {
            Add(HealingDealt);
            return this;
        }

        public HeroSettingCollection AddProjectile(bool hasGravity)
        {
            Add(ProjectileSpeed);
            if (hasGravity) Add(ProjectileGravity);
            return this;
        }

        public HeroSettingCollection AddUlt(string name, bool hasDuration = false, bool hasKnockback = false)
        {
            // Get the names of the settings to be added.
            string isEnabled = "Ultimate Ability";
            string generation = "Ultimate Generation";
            string passive = "Ultimate Generation - Passive";
            string combat = "Ultimate Generation - Combat";
            string knockback = "Knockback Scalar";

            if (name != null)
            {
                // Add the (name) suffix if 'name' is not null.
                isEnabled += " (" + name + ")";
                generation += " (" + name + ")";
                passive += " (" + name + ")";
                combat += " (" + name + ")";
                knockback = name + " " + knockback;
            }

            // Add the settings.
            Add(new SwitchValue(isEnabled, true));
            Add(new RangeValue(generation, 10, 500));
            Add(new RangeValue(passive, 0, 500));
            Add(new RangeValue(combat, 0, 500));

            // Add i18n name resolvers.
            SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.UltimateSwitchSetting    , isEnabled , name ?? isEnabled )); // Toggle
            SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.UltimateGeneration       , generation, name ?? generation)); // Generation
            SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.UltimateGenerationPassive, passive   , name ?? passive   )); // Passive Generation
            SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.UltimateGenerationCombat , combat    , name ?? combat    )); // Combat Generation

            if (hasDuration)
            {
                // Add duration info if it can be changed.
                Add(UltimateDuration);
                Add(InfiniteDuration);
            }

            if (hasKnockback) Add(new RangeValue(knockback, 0, 500));

            return this;
        }

        public HeroSettingCollection AddAbility(string name, bool hasCooldown = true, bool hasKnockback = false, bool rechargeable = false, bool selfKnockback = false)
        {
            Add(new SwitchValue(name, true));

            // If the ability has a cooldown, add the cooldown options.
            if (hasCooldown && !rechargeable)
            {
                string cooldownTimeTitle = name + " Cooldown Time";
                Add(new RangeValue(cooldownTimeTitle, 0, 500));
                SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.CooldownTime, cooldownTimeTitle, name));
            }

            // If the ability has a knockback scalar, add the knockback option.
            if (hasKnockback)
            {
                if (!selfKnockback)
                    Add(new RangeValue(name + " Knockback Scalar", 0, 500));
                else
                {
                    Add(new RangeValue(name + " Knockback Scalar (Enemy)", 0, 300));
                    Add(new RangeValue(name + " Knockback Scalar (Self)", 0, 300));
                }
            }

            // If the ability is rechargeable, add the max time and recharge rate.
            if (rechargeable)
            {
                string maximumTimeTitle = name + " Maximum Time";
                string rechargeRateTitle = name + " Recharge Rate";

                Add(new RangeValue(maximumTimeTitle, 20, 500));
                Add(new RangeValue(rechargeRateTitle, 0, 500));

                SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.CooldownTime, maximumTimeTitle, name));
                SettingNameResolver.AddResolver(new AbilityNameResolver(AbilityNameType.CooldownTime, rechargeRateTitle, name));
            }
            return this;
        }

        public HeroSettingCollection AddSecondaryFire()
        {
            Add(SecondaryFire);
            return this;
        }

        public HeroSettingCollection AddScope()
        {
            Add(NoAutomaticFire);
            Add(NoScope);
            return this;
        }

        public HeroSettingCollection RemoveAmmunition()
        {
            Remove(AmmunitionClipSizeScalar);
            Remove(NoAmmunitionRequirement);
            return this;
        }
    }
}