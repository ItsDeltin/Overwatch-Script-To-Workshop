using System;
using System.Collections.Generic;
using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger.Lobby
{
    public class SettingNameResolver
    {
        private static readonly List<SettingNameResolver> NameResolvers = new List<SettingNameResolver>();
        public string Title { get; }

        public SettingNameResolver(string title)
        {
            Title = title;
        }

        public virtual string ResolveName(WorkshopBuilder builder) => builder.Translate(Title);
        public virtual string[] KeywordInfo() => new string[] { Title };

        public static void AddResolver(SettingNameResolver resolver)
        {
            NameResolvers.Add(resolver);
        }

        public static string ResolveName(WorkshopBuilder builder, string title)
        {
            foreach (SettingNameResolver resolver in NameResolvers)
                if (resolver.Title == title)
                    return resolver.ResolveName(builder);
            return title;
        }

        public static string[] Keywords(string title)
        {
            foreach (SettingNameResolver resolver in NameResolvers)
                if (resolver.Title == title)
                    return resolver.KeywordInfo();
            return new string[] { title };
        }
    }

    public class AbilityNameResolver : SettingNameResolver
    {
        public const string CooldownTime = "%1$s Cooldown Time";
        public const string RechargeRate = "%1$s Recharge Rate";
        public const string MaximumTime = "%1$s Maximum Time";
        public const string UltimateAbility = "Ultimate Ability (%1$s)";
        public const string UltimateGeneration = "Ultimate Generation (%1$s)";
        public const string UltimateGenerationPassive = "Ultimate Generation - Passive (%1$s)";
        public const string UltimateGenerationCombat = "Ultimate Generation - Combat (%1$s)";

        public string AbilityName { get; }
        public AbilityNameType Type { get; }

        public AbilityNameResolver(AbilityNameType type, string settingTitle, string abilityName) : base(settingTitle)
        {
            AbilityName = abilityName;
            Type = type;
        }

        public override string ResolveName(WorkshopBuilder builder)
        {
            switch (Type)
            {
                // (Name) Cooldown Time
                case AbilityNameType.CooldownTime:
                    return SegmentTranslate(builder, CooldownTime);
                
                // (Name) Recharge Rate
                case AbilityNameType.RechargeRate:
                    return SegmentTranslate(builder, RechargeRate);
                
                // (Name) Maximum Time
                case AbilityNameType.MaximumTime:
                    return SegmentTranslate(builder, MaximumTime);

                // Ultimate Ability (Name)
                case AbilityNameType.UltimateSwitchSetting:
                    return SegmentTranslate(builder, UltimateAbility);
                
                // Ultimate Generation (Name)
                case AbilityNameType.UltimateGeneration:
                    return SegmentTranslate(builder, UltimateGeneration);
                
                // Ultimate Generation - Passive (Name)
                case AbilityNameType.UltimateGenerationPassive:
                    return SegmentTranslate(builder, UltimateGenerationPassive);
                
                // Ultimate Generation - Combat (Name)
                case AbilityNameType.UltimateGenerationCombat:
                    return SegmentTranslate(builder, UltimateGenerationCombat);
                
                default: throw new NotImplementedException();
            }
        }

        private string SegmentTranslate(WorkshopBuilder builder, string segmentTitle) 
        {
            if (LanguageInfo.IsKeyword(Title)) return builder.Translate(Title);
            return builder.Translate(segmentTitle).Replace("%1$s", builder.Translate(AbilityName));
        }

        public override string[] KeywordInfo() => new string[] { AbilityName };
    }

    public enum AbilityNameType
    {
        CooldownTime,
        RechargeRate,
        MaximumTime,
        UltimateSwitchSetting,
        UltimateGeneration,
        UltimateGenerationPassive,
        UltimateGenerationCombat
    }
}