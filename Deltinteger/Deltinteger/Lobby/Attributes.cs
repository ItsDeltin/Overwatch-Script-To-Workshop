using System;

namespace Deltin.Deltinteger.Lobby
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    sealed class DefaultValue : Attribute
    {
        public object Value { get; set; }

        public DefaultValue(double defaultValue)
        {
            Value = defaultValue;
        }
        public DefaultValue(bool defaultValue)
        {
            Value = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property)]
    sealed class HasProjectileAttribute : Attribute
    {     
        public bool HasGravity { get; }

        public HasProjectileAttribute(bool hasGravity = false)
        {
            HasGravity = hasGravity;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property)]
    sealed class CanHealAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property)]
    sealed class UltDurationAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property)]
    sealed class NoAmmunitionAttribute : Attribute {}
}