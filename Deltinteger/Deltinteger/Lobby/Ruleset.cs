using System;
using System.Collections.Generic;
using System.Text;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby
{
    public class Ruleset
    {
        public static readonly LobbySetting[] LobbySettings = new LobbySetting[] {
            new SelectValue("Map Rotation", "After A Mirror Match", "After A Game", "Paused"),
            new SelectValue("Return To Lobby", "After A Mirror Match", "After A Game", "Never"),
            new SelectValue("Team Balancing", "Off", "After A Mirror Match", "After A Game"),
            new SwitchValue("Swap Teams After Match", true),
            new RangeValue("Max Team 1 Players", 0, 6, 6),
            new RangeValue("Max Team 2 Players", 0, 6, 6),
            new RangeValue("Max FFA Players", 0, 12, 0),
            new RangeValue("Max Spectators", 0, 12, 2),
            new SwitchValue("Allow Players Who Are In Queue", false),
            new SwitchValue("Use Experimental Update If Available", false),
            new SwitchValue("Match Voice Chat", false),
            new SwitchValue("Pause Game On Player Disconnect", false)
        };

        public HeroesRoot Heroes { get; set; }
        public WorkshopValuePair Lobby { get; set; }

        public void ToWorkshop(StringBuilder builder, OutputLanguage outputLanguage)
        {
        }

        public static void GenerateSchema()
        {
            // Initialize the root.
            RootSchema root = new RootSchema().InitDefinitions().InitProperties();
            root.Schema = "http://json-schema.org/schema";

            root.Properties.Add("Lobby", GetLobby());
            root.Definitions.Add("HeroList", GetHeroList());

            // Get the hero settings.
            RootSchema heroesRoot = new RootSchema("Hero settings.").InitProperties();
            root.Properties.Add("Heroes", heroesRoot);

            heroesRoot.Properties.Add("General", GetHeroListReference("The list of hero settings that affects both teams."));
            heroesRoot.Properties.Add("Team 1", GetHeroListReference("The list of hero settings that affects team 1."));
            heroesRoot.Properties.Add("Team 2", GetHeroListReference("The list of hero settings that affects team 2."));
            
            // Get the result.
            string result = JsonConvert.SerializeObject(root, new JsonSerializerSettings() {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented
            });

            Console.WriteLine(result);
        }

        private static RootSchema GetHeroListReference(string description)
        {
            RootSchema schema = new RootSchema(description);
            schema.Ref = "#/definitions/HeroList";
            return schema;
        }

        private static RootSchema GetHeroList()
        {
            RootSchema schema = new RootSchema().InitProperties();
            foreach (var heroSettings in LobbySettingCollection.AllHeroSettings) schema.Properties.Add(heroSettings.HeroName, heroSettings.GetSchema());
            return schema;
        }

        private static RootSchema GetLobby()
        {
            RootSchema schema = new RootSchema().InitProperties();
            foreach (var lobbySetting in LobbySettings) schema.Properties.Add(lobbySetting.Name, lobbySetting.GetSchema());
            return schema;
        }
    }

    public class HeroesRoot
    {
        [JsonProperty("General")]
        public HeroList General { get; set; }

        [JsonProperty("Team 1")]
        public HeroList Team1 { get; set; }

        [JsonProperty("Team 2")]
        public HeroList Team2 { get; set; }
    }

    public class WorkshopValuePair : Dictionary<String, object> {}
    public class HeroList : Dictionary<String, WorkshopValuePair> {}

    public class LobbySettingCollection : List<LobbySetting>
    {
        // ***********
        // * GLOBALS *
        // ***********
        private static readonly LobbySetting QuickMelee = new SwitchValue("Quick Melee", true);
        private static readonly LobbySetting SpawnWithUlt = new SwitchValue("Spawn With Ultimate Ready", false);
        private static readonly LobbySetting DamageDealt = new RangeValue("Damage Dealt", 10, 500);
        private static readonly LobbySetting DamageRecieved = new RangeValue("Damage Recieved", 10, 500);
        private static readonly LobbySetting HealingDealt = new RangeValue("Healing Dealt", 10, 500);
        private static readonly LobbySetting HealingRecieved = new RangeValue("Healing Recieved", 10, 500);
        private static readonly LobbySetting JumpVerticalSpeed = new RangeValue("Jump Vertical Speed", 25, 800);
        private static readonly LobbySetting MovementGravity = new RangeValue("Movement Gravity", 25, 400);
        private static readonly LobbySetting MovementSpeed = new RangeValue("Movement Speed", 50, 300);
        private static readonly LobbySetting RecieveHeadshotsOnly = new SwitchValue("Recieve Headshots Only", false);
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
        public static readonly LobbySettingCollection[] AllHeroSettings = new LobbySettingCollection[] {
            new LobbySettingCollection("General").AddUlt(null, true).AddProjectile(true).AddHealer(),
            new LobbySettingCollection("Ana").AddUlt("Nano Boost").AddProjectile(false).AddHealer().AddScope().AddAbility("Biotic Grenade").AddAbility("Sleep Dart"),
            new LobbySettingCollection("Ashe").AddUlt("B.O.B.", true).AddProjectile(true).AddScope().AddAbility("Coach Gun", hasKnockback: true, selfKnockback: true).AddAbility("Dynamite").AddRange("Dynamite Fuse Time Scalar", 1),
            new LobbySettingCollection("Baptiste").AddUlt("Amplification Matrix", true).AddProjectile(false).AddHealer().AddAbility("Immortality Field").AddAbility("Regenerative Burst").AddSecondaryFire(),
            new LobbySettingCollection("Bastion").AddUlt("Configuration: Tank", true).AddProjectile(false).AddHealer().AddAbility("Reconfigure", hasCooldown: false).AddAbility("Self-Repair", rechargeable: true),
            new LobbySettingCollection("Brigitte").AddUlt("Rally", true).AddHealer().AddAbility("Repair Pack").AddAbility("Shield Bash", hasKnockback: true).AddAbility("Whip Shot", hasKnockback: true).RemoveAmmunition(),
            new LobbySettingCollection("D.va").AddUlt("Self-Destruct", true).AddAbility("Micro Missiles").AddAbility("Boosters", hasKnockback: true).AddAbility("Defense Matrix", rechargeable: true).RemoveAmmunition()
        };


        /// <summary>The name of the hero.</summary>
        public string HeroName { get; }

        public LobbySettingCollection(string heroName)
        {
            HeroName = heroName;
            AddGlobals();
        }
        
        public LobbySettingCollection AddGlobals()
        {
            Add(QuickMelee);
            Add(SpawnWithUlt);
            Add(DamageDealt);
            Add(DamageRecieved);
            Add(HealingRecieved);
            Add(JumpVerticalSpeed);
            Add(MovementGravity);
            Add(MovementSpeed);
            Add(RecieveHeadshotsOnly);
            Add(PrimaryFire);
            Add(AmmunitionClipSizeScalar);
            Add(NoAmmunitionRequirement);
            return this;
        }

        public LobbySettingCollection AddHealer()
        {
            Add(HealingDealt);
            return this;
        }

        public LobbySettingCollection AddProjectile(bool hasGravity)
        {
            Add(ProjectileSpeed);
            if (hasGravity) Add(ProjectileGravity);
            return this;
        }

        public LobbySettingCollection AddUlt(string name, bool hasDuration = false, bool hasKnockback = false)
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

            if (hasDuration)
            {
                // Add duration info if it can be changed.
                Add(UltimateDuration);
                Add(InfiniteDuration);
            }

            if (hasKnockback) Add(new RangeValue(knockback, 0, 500));

            return this;
        }

        public LobbySettingCollection AddAbility(string name, bool hasCooldown = true, bool hasKnockback = false, bool rechargeable = false, bool selfKnockback = false)
        {
            Add(new SwitchValue(name, true));

            // If the ability has a cooldown, add the cooldown options.
            if (hasCooldown && !rechargeable) Add(new RangeValue(name + " Cooldown Time", 0, 500));

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
                Add(new RangeValue(name + " Maximum Time", 20, 500));
                Add(new RangeValue(name + " Recharge Rate", 0, 500));
            }
            return this;
        }

        public LobbySettingCollection AddSecondaryFire()
        {
            Add(SecondaryFire);
            return this;
        }

        public LobbySettingCollection AddScope()
        {
            Add(NoAutomaticFire);
            Add(NoScope);
            return this;
        }

        public LobbySettingCollection AddRange(string name, double min = 0, double max = 500, double defaultValue = 100)
        {
            Add(new RangeValue(name, min, max, defaultValue));
            return this;
        }

        public LobbySettingCollection AddSwitch(string name, bool defaultValue)
        {
            Add(new SwitchValue(name, defaultValue));
            return this;
        }

        public LobbySettingCollection RemoveAmmunition()
        {
            Remove(AmmunitionClipSizeScalar);
            Remove(NoAmmunitionRequirement);
            return this;
        }

        public new LobbySettingCollection Add(LobbySetting lobbySetting)
        {
            base.Add(lobbySetting);
            return this;
        }

        public RootSchema GetSchema()
        {
            RootSchema schema = new RootSchema($"'{HeroName}' hero settings.").InitProperties();
            foreach (var value in this) schema.Properties.Add(value.Name, value.GetSchema());
            return schema;
        }
    }
}