using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Linq;
using JRange = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace Deltin.Deltinteger.Lobby
{
    public class HeroContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (!ShouldSerialize(property, member))
            {
                property.ShouldDeserialize = i => false;
                property.Ignored = true;
            }

            return property;
        }

        private static bool ShouldSerialize(JsonProperty property, MemberInfo member)
        {
            // Check if the property inherits HeroSettings.
            if (property.DeclaringType.IsAssignableFrom(typeof(HeroSettings)))
            {
                // Check if the hero can heal.
                if (!Compatible<CanHealAttribute>(property, member)) return false;

                // Check if the hero has an ult duration.
                if (!Compatible<UltDurationAttribute>(property, member)) return false;

                // Check ammunition.
                if (Share<NoAmmunitionAttribute>(property, member)) return false;

                // Check if the hero has projectile.
                HasProjectileAttribute heroProjectileInfo = property.DeclaringType.GetCustomAttribute<HasProjectileAttribute>();
                HasProjectileAttribute valueProjectileInfo = member.GetCustomAttribute<HasProjectileAttribute>();
                if (valueProjectileInfo != null && (heroProjectileInfo == null || valueProjectileInfo.HasGravity && !valueProjectileInfo.HasGravity)) return false;
            }
            return true;
        }

        private static bool Compatible<T>(JsonProperty property, MemberInfo member) where T: Attribute
        {
            bool heroHasAttribute = property.DeclaringType.GetCustomAttribute<T>() != null;
            bool valueHasAttribute = member.GetCustomAttribute<T>() != null;

            if (!heroHasAttribute && valueHasAttribute)
                return false;
            else
                return true;
        }

        private static bool Share<T>(JsonProperty property, MemberInfo member) where T: Attribute
        {
            bool heroHasAttribute = property.DeclaringType.GetCustomAttribute<T>() != null;
            bool valueHasAttribute = member.GetCustomAttribute<T>() != null;
            return valueHasAttribute && heroHasAttribute;
        }
    }

    class HeroSchemaProvider : JSchemaGenerationProvider
    {
        public override JSchema GetSchema(JSchemaTypeGenerationContext context)
        {
            if (context.MemberProperty == null || context.MemberProperty.DeclaringType != typeof(HeroSettings)) return null;

            // Get the SettingInfo attribute.
            DefaultValue settingInfo = context.MemberProperty.DeclaringType.GetProperty(context.MemberProperty.UnderlyingName).GetCustomAttribute<DefaultValue>();

            // Create the generator and schema.
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema schema = generator.Generate(context.ObjectType);

            if (settingInfo != null)
            {
                // Get the default value.
                schema.Default = JToken.FromObject(settingInfo.Value);
            }
            return schema;
        }
    }

    public class HeroSettings
    {
        [DefaultValue(true)]
        public bool QuickMelee { get; set; }

        [DefaultValue(false)]
        public bool SpawnWithUltimateReady { get; set; }

        [DefaultValue(100)]
        [JRange(10, 500)]
        public double DamageDealt { get; set; }

        [DefaultValue(100)]
        [JRange(10, 500)]
        public double DamageRecieved { get; set; }

        [DefaultValue(100)]
        [JRange(10, 500)]
        [CanHeal]
        public double HealingDealt { get; set; }

        [DefaultValue(100)]
        [JRange(10, 500)]
        public double HealingRecieved { get; set; }

        [DefaultValue(100)]
        [JRange(10, 500)]
        public double Health { get; set; }

        [DefaultValue(100)]
        [JRange(25, 800)]
        public double JumpVerticalSpeed { get; set; }

        [DefaultValue(100)]
        [JRange(25, 400)]
        public double MovementGravity { get; set; }

        [DefaultValue(100)]
        [JRange(50, 300)]
        public double MovementSpeed { get; set; }

        [DefaultValue(100)]
        [JRange(0, 500)]
        [HasProjectile(true)]
        public double ProjectileGravity { get; set; }

        [DefaultValue(100)]
        [JRange(0, 500)]
        [HasProjectile]
        public double ProjectileSpeed { get; set; }

        [DefaultValue(false)]
        public bool RecieveHeadshotsOnly { get; set; }

        [DefaultValue(true)]
        [NoAmmunition]
        public bool PrimaryFire { get; set; }

        [DefaultValue(100)]
        [JRange(25, 500)]
        [NoAmmunition]
        public double AmmunitionClipSizeScalar { get; set; }

        [DefaultValue(false)]
        [NoAmmunition]
        public bool NoAmmunitionRequirement { get; set; }
    }

    [HasProjectile(true)]
    public class GeneralSettings : HeroSettings {}

    [HasProjectile]
    [CanHeal]
    public class AnaSettings : HeroSettings {}

    [HasProjectile(true)]
    [UltDurationAttribute]
    public class AsheSettings : HeroSettings {}
}