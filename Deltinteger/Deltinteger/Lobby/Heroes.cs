using System.Linq;
using Newtonsoft.Json.Linq;

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
        public static HeroSettingCollection[] AllHeroSettings { get; private set; }


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
            Add(Health);
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

            // i18n name resolvers.
            AbilityNameResolver enabledResolver = new AbilityNameResolver(AbilityNameType.UltimateSwitchSetting, isEnabled, name ?? isEnabled); // Toggle
            AbilityNameResolver generationResolver = new AbilityNameResolver(AbilityNameType.UltimateGeneration, generation, name ?? generation); // Generation
            AbilityNameResolver passiveGenerationResolver = new AbilityNameResolver(AbilityNameType.UltimateGenerationPassive, passive, name ?? passive); // Passive Generation
            AbilityNameResolver combatGenerationResolver = new AbilityNameResolver(AbilityNameType.UltimateGenerationCombat, combat, name ?? combat); // Combat Generation

            // Add the settings.
            Add(new SwitchValue(isEnabled, true) { TitleResolver = enabledResolver });
            Add(RangeValue.NewPercentage(generation, generationResolver, min:10, max:500));
            Add(RangeValue.NewPercentage(passive, passiveGenerationResolver, min:0, max:500));
            Add(RangeValue.NewPercentage(combat, combatGenerationResolver, min:0, max:500));

            if (hasDuration)
            {
                // Add duration info if it can be changed.
                Add(UltimateDuration);
                Add(InfiniteDuration);
            }

            if (hasKnockback) Add(RangeValue.NewPercentage(knockback, min:0, max:500));

            return this;
        }

        public HeroSettingCollection AddAbility(string name, bool hasCooldown = true, bool hasKnockback = false, bool rechargeable = false, bool noMaximumTime = false, bool selfKnockback = false)
        {
            Add(new SwitchValue(name, true));

            // If the ability has a cooldown, add the cooldown options.
            if (hasCooldown)
            {
                string cooldownTimeTitle = name + " Cooldown Time";
                Add(RangeValue.NewPercentage(cooldownTimeTitle, new AbilityNameResolver(AbilityNameType.CooldownTime, cooldownTimeTitle, name), min:0, max:500));
            }

            // If the ability has a knockback scalar, add the knockback option.
            if (hasKnockback)
            {
                if (!selfKnockback)
                    Add(RangeValue.NewPercentage(name + " Knockback Scalar", min:0, max:500));
                else
                {
                    Add(RangeValue.NewPercentage(name + " Knockback Scalar (Enemy)", min:0, max:300));
                    Add(RangeValue.NewPercentage(name + " Knockback Scalar (Self)", min:0, max:300));
                }
            }

            // If the ability is rechargeable, add the max time and recharge rate.
            if (rechargeable)
            {
                string rechargeRateTitle = name + " Recharge Rate";
                Add(RangeValue.NewPercentage(rechargeRateTitle, new AbilityNameResolver(AbilityNameType.CooldownTime, rechargeRateTitle, name), min:0, max:500));

                if (!noMaximumTime)
                {
                    string maximumTimeTitle = name + " Maximum Time";
                    Add(RangeValue.NewPercentage(maximumTimeTitle, new AbilityNameResolver(AbilityNameType.CooldownTime, maximumTimeTitle, name), min:20, max:500));
                }
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

        public static void Init()
        {
            AllHeroSettings = new HeroSettingCollection[] {
                new HeroSettingCollection("General").AddUlt(null, true).AddProjectile(true).AddHealer(),
                new HeroSettingCollection("Ana").AddUlt("Nano Boost").AddProjectile(false).AddHealer().AddScope().AddAbility("Biotic Grenade").AddAbility("Sleep Dart"),
                new HeroSettingCollection("Ashe").AddUlt("B.O.B.", true).AddProjectile(true).AddScope().AddAbility("Coach Gun", hasKnockback: true, selfKnockback: true).AddAbility("Dynamite").AddRange("Dynamite Fuse Time Scalar", 1),
                new HeroSettingCollection("Baptiste").AddUlt("Amplification Matrix", true).AddProjectile(false).AddHealer().AddAbility("Immortality Field").AddAbility("Regenerative Burst").AddSecondaryFire(),
                new HeroSettingCollection("Bastion").AddUlt("Configuration: Tank", true).AddProjectile(false).AddHealer().AddAbility("Reconfigure", hasCooldown: false).AddAbility("Self-Repair", hasCooldown: false, rechargeable: true),
                new HeroSettingCollection("Brigitte").AddUlt("Rally", true).AddHealer().AddAbility("Barrier Shield", rechargeable: true).AddAbility("Repair Pack").AddAbility("Shield Bash", hasKnockback: true).AddAbility("Whip Shot", hasKnockback: true).RemoveAmmunition(),
                new HeroSettingCollection("D.va").AddUlt("Self-Destruct", true).AddAbility("Micro Missiles").AddAbility("Boosters", hasKnockback: true).AddAbility("Defense Matrix", hasCooldown: false, rechargeable: true).AddRange("Call Mech Knockback Scalar", 0, 400).AddSwitch("Spawn Without Mech", false).RemoveAmmunition(),
                new HeroSettingCollection("Doomfist").AddUlt("Meteor Strike", hasKnockback: true, hasDuration: true).AddProjectile(false).AddAbility("Rising Uppercut", hasKnockback: true).AddAbility("Rocket Punch", hasKnockback: true).AddAbility("Seismic Slam").AddRange("Ammunition Regeneration Time Scalar", 33, 500),
                new HeroSettingCollection("Echo").AddUlt("Duplicate").AddProjectile(false).AddAbility("Flight").AddAbility("Focusing Beam").AddAbility("Glide", hasCooldown: false).AddAbility("Sticky Bombs"),
                new HeroSettingCollection("Genji").AddUlt("Dragonblade", hasDuration: true).AddProjectile(false).AddSecondaryFire().AddAbility("Deflect").AddAbility("Swift Strike"),
                new HeroSettingCollection("Hanzo").AddUlt("Dragonstrike").AddProjectile(true).RemoveAmmunition().AddAbility("Lunge").AddRange("Lunge Distance Scalar", 20, 300).AddAbility("Sonic Arrow").AddAbility("Storm Arrows").AddIntRange("Storm Arrows Quantity", false, 3, 12, 5),
                new HeroSettingCollection("Junkrat").AddUlt("Rip-Tire", hasDuration: true).AddProjectile(true).AddAbility("Concussion Mine", hasKnockback: true).AddAbility("Steel Trap").AddRange("Frag Launcher Knockback Scalar", 0, 400),
                new HeroSettingCollection("Lúcio").AddUlt("Sound Barrier").AddHealer().AddProjectile(false).AddAbility("Amp It Up").AddAbility("Crossfade", hasCooldown: false).AddAbility("Soundwave", hasKnockback: true),
                new HeroSettingCollection("Mccree").AddUlt("Deadeye").AddProjectile(false).AddSecondaryFire().AddAbility("Combat Roll").AddAbility("Flashbang"),
                new HeroSettingCollection("Mei").AddUlt("Blizzard").AddProjectile(true).AddSecondaryFire().AddHealer().AddAbility("Cryo-Freeze").AddAbility("Ice Wall").AddRange("Blizzard Freeze Minimum", 0, 100, 50).AddRange("Blizzard Freeze Rate Scalar").AddSwitch("Freeze Stacking", false).AddRange("Weapon Freeze Duration Scalar", 20).AddRange("Weapon Freeze Minimum", 0, 100, 30).AddRange("Weapon Freeze Rate Scalar", 25),
                new HeroSettingCollection("Mercy").AddUlt("Valkyrie").AddProjectile(false).AddSecondaryFire().AddHealer().AddAbility("Guardian Angel").AddAbility("Regeneration", hasCooldown: false).AddAbility("Resurrect").AddSelectRef("Weapons Enabled Mercy", "Weapons Enabled", "All", "Caduceus Staff Only", "Caduceus Blaster Only"),
                new HeroSettingCollection("Moira").AddUlt("Coalescence", hasDuration: true).AddProjectile(false).AddSecondaryFire().RemoveAmmunition().AddHealer().AddAbility("Fade").AddAbility("Biotic Orb").AddRange("Biotic Orb Max Damage Scalar", 10).AddRange("Biotic Orb Max Healing Scalar", 10).AddRange("Biotic Energy Maximum", 20).AddRange("Biotic Energy Recharge Rate"),
                new HeroSettingCollection("Orisa").AddUlt("Supercharger").AddProjectile(true).AddAbility("Fortify").AddAbility("Halt!").AddAbility("Protective Barrier"),
                new HeroSettingCollection("Pharah").AddUlt("Barrage").AddProjectile(false).AddAbility("Concussive Blast", hasKnockback: true).AddAbility("Hover Jets", hasCooldown: false, rechargeable: true).AddRange("Hover Jets Vertical Speed Scalar", 25, 300).AddSwitch("Hover Jets Unlimited Fuel", false).AddAbility("Jump Jet").AddRange("Jump Jet Acceleration Scalar", 25, 300).AddRange("Rocket Launcher Knockback Scalar", 0, 400),
                new HeroSettingCollection("Reaper").AddUlt("Death Blossom").AddHealer().AddAbility("Shadow Step").AddAbility("Wraith Form"),
                new HeroSettingCollection("Reinhardt").AddUlt("Earthshatter").AddProjectile(false).RemoveAmmunition().AddAbility("Barrier Field", rechargeable: true, noMaximumTime: true).AddAbility("Charge", hasKnockback: true).AddAbility("Fire Strike").AddRange("Rocket Hammer Knockback Scalar", 0, 400),
                new HeroSettingCollection("Roadhog").AddUlt("Whole Hog", hasKnockback: true).AddHealer().AddProjectile(false).AddSecondaryFire().AddAbility("Chain Hook").AddAbility("Take A Breather"),
                new HeroSettingCollection("Sigma").AddUlt("Gravitic Flux").RemoveAmmunition().AddProjectile(true).AddAbility("Accretion", hasKnockback: true).AddAbility("Experimental Barrier", rechargeable: true).AddAbility("Kinetic Grasp"),
                new HeroSettingCollection("Soldier: 76").AddUlt("Tactical Visor", hasDuration: true).AddHealer().AddProjectile(false).AddAbility("Biotic Field").AddAbility("Helix Rockets", hasKnockback: true).AddAbility("Sprint", hasCooldown: false),
                new HeroSettingCollection("Sombra").AddUlt("EMP").AddProjectile(true).AddAbility("Hack").AddAbility("Stealth").AddAbility("Translocator"),
                new HeroSettingCollection("Symmetra").AddUlt("Photon Barrier").AddProjectile(false).AddSecondaryFire().AddAbility("Sentry Turret").AddAbility("Teleporter"),
                new HeroSettingCollection("Torbjörn").AddUlt("Molten Core", hasDuration: true).AddProjectile(true).AddSecondaryFire().AddAbility("Deploy Turret").AddAbility("Overload").AddRange("Overload Duration Scalar").AddSelectRef("Weapons Enabled Torbjörn", "Weapons Enabled", "All", "Rivet Gun Only", "Forge Hammer Only"),
                new HeroSettingCollection("Tracer").AddUlt("Pulse Bomb").AddProjectile(true).AddAbility("Blink").AddAbility("Recall"),
                new HeroSettingCollection("Widowmaker").AddUlt("Infra-sight", hasDuration: true).AddProjectile(true).AddScope().AddAbility("Grappling Hook").AddAbility("Venom Mine"),
                new HeroSettingCollection("Winston").AddUlt("Primal Rage", hasDuration: true).AddRange("Primal Rage Melee Knockback Scalar", 25, 300).AddAbility("Barrier Projector").AddAbility("Jump Pack", hasKnockback: true).AddRange("Jump Pack Acceleration Scalar", 25, 300),
                new HeroSettingCollection("Wrecking Ball").AddUlt("Minefield", hasDuration: true, hasKnockback: true).AddProjectile(true).AddAbility("Adaptive Shield").AddAbility("Grappling Claw", hasKnockback: true).AddAbility("Piledriver").AddAbility("Roll", hasCooldown: false).AddSwitch("Roll Always Active", false),
                new HeroSettingCollection("Zarya").AddUlt("Graviton Surge").AddProjectile(true).AddSecondaryFire().AddAbility("Particle Barrier").AddAbility("Projected Barrier").AddRange("Particle Cannon Secondary Knockback Scalar", 0, 400),
                new HeroSettingCollection("Zenyatta").AddUlt("Transcendence").AddProjectile(false).AddSecondaryFire().AddHealer().AddAbility("Orb Of Harmony", hasCooldown: false).AddAbility("Orb Of Discord", hasCooldown: false)
            };
        }

        public static void Validate(SettingValidation validation, JObject heroes)
        {
            foreach (JProperty property in heroes.Properties())
            {
                if (property.Name == "Enabled Heroes" || property.Name == "Disabled Heroes") continue;
                HeroSettingCollection relatedHeroCollection = AllHeroSettings.FirstOrDefault(hs => hs.HeroName == property.Name);

                if (relatedHeroCollection == null) validation.InvalidSetting(property.Name);
                else Ruleset.ValidateSetting(validation, relatedHeroCollection, property.Value);
            }
        }
    }
}
