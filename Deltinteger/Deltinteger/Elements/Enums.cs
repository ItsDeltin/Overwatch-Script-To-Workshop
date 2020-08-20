using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.I18n;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Elements
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class WorkshopEnum : Attribute {}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum)]
    public class EnumOverride : Attribute
    {
        public EnumOverride(string codeName = null, string workshopName = null)
        {
            CodeName = codeName;
            WorkshopName = workshopName;
        }
        public string CodeName { get; }
        public string WorkshopName { get; }
    }

    public class EnumData
    {
        private static EnumData[] AllEnums = null;

        public static EnumData[] GetEnumData()
        {
            if (AllEnums == null)
            {
                Type[] enums = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<WorkshopEnum>() != null).ToArray();
                AllEnums = new EnumData[enums.Length];

                for (int i = 0; i < AllEnums.Length; i++)
                    AllEnums[i] = new EnumData(enums[i]);
            }
            return AllEnums;
        }

        public static EnumData GetEnum(Type type)
        {
            return GetEnumData().FirstOrDefault(e => e.Type == type);
        }

        public static EnumData GetEnum<T>()
        {
            return GetEnum(typeof(T));
        }

        public static EnumMember GetEnumValue(object enumValue)
        {
            return GetEnum(enumValue.GetType()).GetEnumMember(enumValue.ToString());
        }

        public static Element ToElement(EnumMember enumMember)
        {
            // This converts enums with special properties to an Element.

            switch(enumMember.Enum.CodeName)
            {
                case "Hero": return Element.Part<V_HeroVar>(enumMember);
                case "Team": return Element.Part<V_TeamVar>(enumMember);
                case "Map": return Element.Part<V_MapVar>(enumMember);
                case "GameMode": return Element.Part<V_GameModeVar>(enumMember);
                default: return null;
            }
        }

        public string CodeName { get; private set; }
        public EnumMember[] Members { get; private set; }
        public Type Type { get; private set; }
        public Type UnderlyingType { get; private set; }

        public EnumData(Type type)
        {
            EnumOverride data = type.GetCustomAttribute<EnumOverride>();
            CodeName = data?.CodeName ?? type.Name;

            Type = type;
            UnderlyingType = Enum.GetUnderlyingType(type);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            Members = new EnumMember[fields.Length];
            var values = Enum.GetValues(type);

            for (int v = 0; v < Members.Length; v++)
            {
                EnumOverride fieldData = fields[v].GetCustomAttribute<EnumOverride>();
                string fieldCodeName     = fieldData?.CodeName     ?? fields[v].Name;
                string fieldWorkshopName = fieldData?.WorkshopName ?? Extras.AddSpacesToSentence(fields[v].Name.Replace('_', ' '), false);
                bool isHidden = fields[v].GetCustomAttribute<HideElement>() != null;

                Members[v] = new EnumMember(this, fieldCodeName, fieldWorkshopName, values.GetValue(v), isHidden);
            }
        }

        public bool ConvertableToElement()
        {
            return new string[] { "Hero", "Team", "Map", "GameMode" }.Contains(CodeName);
        }

        public bool IsEnumMember(string codeName)
        {
            return GetEnumMember(codeName) != null;
        }

        public EnumMember GetEnumMember(string codeName)
        {
            return Members.FirstOrDefault(m => m.CodeName == codeName);
        }
    }

    public class EnumMember : IWorkshopTree
    {
        public EnumData @Enum { get; }
        public string CodeName { get; }
        public string WorkshopName { get; }
        public object UnderlyingValue { get; }
        public object Value { get; }
        public bool IsHidden { get; }

        public EnumMember(EnumData @enum, string codeName, string workshopName, object value, bool isHidden)
        {
            @Enum = @enum;
            CodeName = codeName;
            WorkshopName = workshopName;
            UnderlyingValue = System.Convert.ChangeType(value, @Enum.UnderlyingType);
            Value = value;
            IsHidden = isHidden;
        }

        public string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            string numTranslate(string name)
            {
                return LanguageInfo.Translate(language, name) + WorkshopName.Substring(name.Length);
            }

            if (@Enum.Type == typeof(PlayerSelector) && WorkshopName.StartsWith("Slot")) return numTranslate("Slot");
            if (@Enum.Type == typeof(Button) && WorkshopName.StartsWith("Ability")) return numTranslate("Ability");
            if ((@Enum.Type == typeof(Team) || @Enum.Type == typeof(Color)) && WorkshopName.StartsWith("Team")) return numTranslate("Team");
            
            return LanguageInfo.Translate(language, WorkshopName).RemoveStructuralChars();
        }

        public string GetI18nKeyword()
        {
            if (@Enum.Type == typeof(PlayerSelector) && WorkshopName.StartsWith("Slot")) return "Slot";
            if (@Enum.Type == typeof(Button) && WorkshopName.StartsWith("Ability")) return "Ability";
            if ((@Enum.Type == typeof(Team) || @Enum.Type == typeof(Color)) && WorkshopName.StartsWith("Team")) return "Team";
            return WorkshopName;
        }

        public bool EqualTo(IWorkshopTree b)
        {
            if (this.GetType() != b.GetType()) return false;

            EnumMember bAsEnum = (EnumMember)b;
            return WorkshopName == bAsEnum.WorkshopName && Enum == bAsEnum.Enum;
        }
    }

    [WorkshopEnum]
    [EnumOverride("Event", null)]
    public enum RuleEvent
    {
        [EnumOverride(null, "Ongoing - Global")]
        OngoingGlobal,
        [EnumOverride(null, "Ongoing - Each Player")]
        OngoingPlayer,

        [EnumOverride(null, "Player earned elimination")]
        OnElimination,
        [EnumOverride(null, "Player dealt final blow")]
        OnFinalBlow,

        [EnumOverride(null, "Player dealt damage")]
        OnDamageDealt,
        [EnumOverride(null, "Player took damage")]
        OnDamageTaken,

        [EnumOverride(null, "Player died")]
        OnDeath,

        [EnumOverride(null, "Player Dealt Healing")]
        OnHealingDealt,
        [EnumOverride(null, "Player Received Healing")]
        OnHealingTaken,

        [EnumOverride(null, "Player Joined Match")]
        OnPlayerJoin,
        [EnumOverride(null, "Player Left Match")]
        OnPlayerLeave,
        [HideElement]
        Subroutine,
        [EnumOverride(null, "Player Dealt Knockback")]
        PlayerDealtKnockback,
        [EnumOverride(null, "Player Received Knockback")]
        PlayerReceivedKnockback
    }

    [WorkshopEnum]
    [EnumOverride("PlayerSelector", null)]
    public enum PlayerSelector
    {
        All,
        Slot0,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        Slot10,
        Slot11,
        Reaper,
        Tracer,
        Mercy,
        Hanzo,
        [EnumOverride(null, "Torbjörn")]
        Torbjorn,
        Reinhardt,
        Pharah,
        Winston,
        Widowmaker,
        Bastion,
        Symmetra,
        Zenyatta,
        Genji,
        Roadhog,
        Mccree,
        Junkrat,
        Zarya,
        [EnumOverride(null, "Soldier: 76")]
        Soldier76,
        [EnumOverride(null, "Lúcio")]
        Lucio,
        [EnumOverride(null, "D.va")]
        Dva,
        Mei,
        Sombra,
        Doomfist,
        Ana,
        Orisa,
        Brigitte,
        Moira,
        WreckingBall,
        Ashe,
        Echo,
        Baptiste,
        Sigma
    }

    [WorkshopEnum]
    public enum Hero
    {
        Ana,
        Ashe,
        Baptiste,
        Bastion,
        Brigitte,
        [EnumOverride(null, "D.va")]
        Dva,
        Doomfist,
        Echo,
        Genji,
        Hanzo,
        Junkrat,
        [EnumOverride(null, "Lúcio")]
        Lucio,
        Mccree,
        Mei,
        Mercy,
        Moira,
        Orisa,
        Pharah,
        Reaper,
        Reinhardt,
        Roadhog,
        Sigma,
        [EnumOverride(null, "Soldier: 76")]
        Soldier76,
        Sombra,
        Symmetra,
        [EnumOverride(null, "Torbjörn")]
        Torbjorn,
        Tracer,
        Widowmaker,
        Winston,
        WreckingBall,
        Zarya,
        Zenyatta
    }

    [WorkshopEnum]
    public enum Operators
    {
        [EnumOverride(null, "==")]
        Equal,
        [EnumOverride(null, "!=")]
        NotEqual,
        [EnumOverride(null, "<")]
        LessThan,
        [EnumOverride(null, "<=")]
        LessThanOrEqual,
        [EnumOverride(null, ">")]
        GreaterThan,
        [EnumOverride(null, ">=")]
        GreaterThanOrEqual
    }

    [WorkshopEnum]
    public enum Operation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        RaiseToPower,
        Min,
        Max,
        AppendToArray,
        RemoveFromArrayByValue,
        RemoveFromArrayByIndex
    }

    [WorkshopEnum]
    public enum Button
    {
        PrimaryFire,
        SecondaryFire,
        [EnumOverride(null, "Ability 1")]
        Ability1,
        [EnumOverride(null, "Ability 2")]
        Ability2,
        Ultimate,
        Interact,
        Jump,
        Crouch,
        Melee,
        Reload
    }

    [WorkshopEnum]
    public enum Relative
    {
        ToWorld,
        ToPlayer
    }

    [WorkshopEnum]
    public enum ContraryMotion
    {
        [EnumOverride(null, "Cancel Contrary Motion")]
        Cancel,
        [EnumOverride(null, "Incorporate Contrary Motion")]
        Incorporate
    }

    [WorkshopEnum]
    public enum RateChaseReevaluation
    {
        [EnumOverride(null, "Destination and Rate")]
        DestinationAndRate,
        None
    }

    [WorkshopEnum]
    public enum TimeChaseReevaluation
    {
        DestinationAndDuration,
        None
    }

    [WorkshopEnum]
    public enum Status
    {
        Hacked,
        Burning,
        KnockedDown,
        Asleep,
        Frozen,
        Unkillable,
        Invincible,
        PhasedOut,
        Rooted,
        Stunned
    }

    [WorkshopEnum]
    public enum Team
    {
        All,
        Team1,
        Team2,
    }

    [WorkshopEnum]
    public enum WaitBehavior
    {
        IgnoreCondition,
        AbortWhenFalse,
        RestartWhenTrue
    }

    [WorkshopEnum]
    public enum Effect
    {
        Sphere,
        LightShaft,
        Orb,
        Ring,
        Cloud,
        Sparkles,
        GoodAura,
        BadAura,
        EnergySound,
        [EnumOverride(null, "Pick-up Sound")]
        PickupSound,
        GoodAuraSound,
        BadAuraSound,
        SparklesSound,
        SmokeSound,
        DecalSound,
        BeaconSound
    }

    [WorkshopEnum]
    public enum Color
    {
        White,
        Yellow,
        Green,
        Purple,
        Red,
        Blue,
        [EnumOverride(null, "Team 1")]
        Team1,
        [EnumOverride(null, "Team 2")]
        Team2,
        Aqua,
        Orange,
        SkyBlue,
        Turquoise,
        LimeGreen
    }

    [WorkshopEnum]
    public enum EffectRev
    {
        [EnumOverride(null, "Visible To Position and Radius")]
        VisibleToPositionAndRadius,
        PositionAndRadius,
        VisibleTo,
        None
    }

    [WorkshopEnum]
    public enum Rounding
    {
        Up,
        Down,
        [EnumOverride(null, "To Nearest")]
        Nearest
    }

    [WorkshopEnum]
    public enum Communication
    {
        VoiceLineUp,
        VoiceLineLeft,
        VoiceLineRight,
        VoiceLineDown,
        EmoteUp,
        EmoteLeft,
        EmoteRight,
        EmoteDown,
        UltimateStatus,
        Hello,
        NeedHealing,
        GroupUp,
        Thanks,
        Acknowledge,
        PressTheAttack,
        YouAreWelcome,
        Yes,
        No,
        Goodbye,
        Go,
        Ready,
        FallBack,
        PushForward,
        Incoming,
        WithYou,
        GoingIn,
        OnMyWay,
        Attacking,
        Defending,
        NeedHelp,
        Sorry,
        Countdown,
    }

    [WorkshopEnum]
    [EnumOverride("Location", null)]
    public enum HudLocation
    {
        Left,
        Top,
        Right
    }

    [WorkshopEnum]
    public enum ObjectiveRev
    {
        [EnumOverride(null, "Visible To and String")]
        VisibleToAndString,
        String,
        [EnumOverride(null, "Visible To Sort Order and String")]
        VisibleToSortOrderAndString,
        SortOrderAndString,
        VisibleToAndSortOrder,
        VisibleTo,
        SortOrder,
        None
    }

    [WorkshopEnum]
    public enum HudTextRev
    {
        [EnumOverride(null, "Visible To and String")]
        VisibleToAndString,
        String,
        [EnumOverride(null, "Visible To Sort Order and String")]
        VisibleToSortOrderAndString,
        SortOrderAndString,
        VisibleToAndSortOrder,
        VisibleTo,
        SortOrder,
        None
    }

    [WorkshopEnum]
    public enum Icon
    {
        [EnumOverride(null, "Arrow: Down")]
        ArrowDown,
        [EnumOverride(null, "Arrow: Left")]
        ArrowLeft,
        [EnumOverride(null, "Arrow: Right")]
        ArrowRight,
        [EnumOverride(null, "Arrow: Up")]
        ArrowUp,
        Asterisk,
        Bolt,
        Checkmark,
        Circle,
        Club,
        Diamond,
        Dizzy,
        ExclamationMark,
        Eye,
        Fire,
        Flag,
        Halo,
        Happy,
        Heart,
        Moon,
        No,
        Plus,
        Poison,
        [EnumOverride(null, "Poison 2")]
        Poison2,
        QuestionMark,
        Radioactive,
        Recycle,
        RingThick,
        RingThin,
        Sad,
        Skull,
        Spade,
        Spiral,
        Stop,
        Trashcan,
        Warning,
        X
    }

    [WorkshopEnum]
    public enum IconRev
    {
        VisibleToAndPosition,
        Position,
        VisibleTo,
        None
    }

    [WorkshopEnum]
    public enum PlayEffect
    {
        GoodExplosion,
        BadExplosion,
        RingExplosion,
        GoodPickupEffect,
        BadPickupEffect,
        DebuffImpactSound,
        BuffImpactSound,
        RingExplosionSound,
        BuffExplosionSound,
        ExplosionSound
    }

    [WorkshopEnum]
    public enum InvisibleTo
    {
        All,
        Enemies,
        None
    }

    [WorkshopEnum]
    public enum AccelerateRev
    {
        [EnumOverride(null, "Direction Rate and Max Speed")]
        DirectionRateAndMaxSpeed,
        None
    }

    [WorkshopEnum]
    public enum DamageModificationRev
    {
        [EnumOverride(null, "Receivers, Damagers, and Damage Percent")]
        ReceiversDamagersAndDamagePercent,
        ReceiversAndDamagers,
        None
    }

    [WorkshopEnum]
    public enum HealingModificationRev
    {
        [EnumOverride(null, "Receivers, Healers, and Healing Percent")]
        ReceiversDamagersAndDamagePercent,
        ReceiversAndHealers,
        None
    }

    [WorkshopEnum]
    public enum IfAlreadyExecuting
    {
        RestartRule,
        DoNothing
    }

    [WorkshopEnum]
    public enum FacingRev
    {
        [EnumOverride(null, "Direction and Turn Rate")]
        DirectionAndTurnRate,
        None
    }

    [WorkshopEnum]
    public enum BarrierLOS
    {
        [EnumOverride(null, "Barriers Do Not Block LOS")]
        NoBarriersBlock,
        [EnumOverride(null, "Enemy Barriers Block LOS")]
        EnemyBarriersBlock,
        [EnumOverride(null, "All Barriers Block LOS")]
        AllBarriersBlock
    }

    [WorkshopEnum]
    public enum Transformation
    {
        Rotation,
        RotationAndTranslation
    }

    [WorkshopEnum]
    public enum RadiusLOS
    {
        Off,
        Surfaces,
        SurfacesAndEnemyBarriers,
        SurfacesAndAllBarriers
    }

    [WorkshopEnum]
    public enum LocalVector
    {
        Rotation,
        RotationAndTranslation
    }

    [WorkshopEnum]
    public enum Clipping
    {
        ClipAgainstSurfaces,
        DoNotClip
    }

    [WorkshopEnum]
    public enum InworldTextRev
    {
        [EnumOverride(null, "Visible To Position and String")]
        VisibleToPositionAndString,
        [EnumOverride(null, "Visible To and String")]
        VisibleToAndString,
        String,
        VisibleToAndPosition,
        VisibleTo,
        None
    }

    [WorkshopEnum]
    public enum BeamType
    {
        GoodBeam,
        BadBeam,
        GrappleBeam
    }

    [WorkshopEnum]
    public enum Spectators
    {
        DefaultVisibility,
        VisibleAlways,
        VisibleNever
    }

    [WorkshopEnum]
    public enum ThrottleBehavior
    {
        [EnumOverride(null, "Replace existing throttle")]
        ReplaceExistingThrottle,
        AddToExistingThrottle
    }

    [WorkshopEnum]
    public enum ThrottleRev
    {
        [EnumOverride(null, "Direction and Magnitude")]
        DirectionAndMagnitude,
        None
    }

    [WorkshopEnum]
    public enum Map
    {
        Ayutthaya,
        Black_Forest,
        [EnumOverride(null, "Black Forest (Winter)")]
        Black_Forest_Winter,
        Blizzard_World,
        [EnumOverride(null, "Blizzard World (Winter)")]
        Blizzard_World_Winter,
        Busan,
        [EnumOverride(null, "Busan Downtown (Lunar New Year)")]
        Busan_Downtown_Lunar,
        [EnumOverride(null, "Busan Sanctuary (Lunar New Year)")]
        Busan_Sanctuary_Lunar,
        Busan_Stadium,
        Castillo,
        [EnumOverride(null, "Château Guillard")]
        Chateau_Guillard,
        [EnumOverride(null, "Château Guillard (Halloween)")]
        Chateau_Guillard_Halloween,
        Dorado,
        [EnumOverride(null, "Ecopoint: Antarctica")]
        Ecopoint_Antarctica,
        [EnumOverride(null, "Ecopoint: Antarctica (Winter)")]
        Ecopoint_Antarctica_Winter,
        Eichenwalde,
        [EnumOverride(null, "Eichenwalde (Halloween)")]
        Eichenwalde_Halloween,
        [EnumOverride(null, "Estádio das Rãs")]
        Estadio_Das_Ras,
        Hanamura,
        [EnumOverride(null, "Hanamura (Winter)")]
        Hanamura_Winter,
        Havana,
        Hollywood,
        [EnumOverride(null, "Hollywood (Halloween)")]
        Hollywood_Halloween,
        Horizon_Lunar_Colony,
        Ilios,
        Ilios_Lighthouse,
        Ilios_Ruins,
        Ilios_Well,
        [EnumOverride(null, "Junkenstein's Revenge")]
        Junkensteins_Revenge,
        Junkertown,
        [EnumOverride(null, "King's Row")]
        Kings_Row,
        [EnumOverride(null, "King's Row (Winter)")]
        Kings_Row_Winter,
        Lijiang_Control_Center,
        [EnumOverride(null, "Lijiang Control Center (Lunar New Year)")]
        Lijiang_Control_Center_Lunar,
        Lijiang_Garden,
        [EnumOverride(null, "Lijiang Garden (Lunar New Year)")]
        Lijiang_Garden_Lunar,
        Lijiang_Night_Market,
        [EnumOverride(null, "Lijiang Night Market (Lunar New Year)")]
        Lijiang_Night_Market_Lunar,
        Lijiang_Tower,
        [EnumOverride(null, "Lijiang Tower (Lunar New Year)")]
        Lijiang_Tower_Lunar,
        Necropolis,
        Nepal,
        Nepal_Sanctum,
        Nepal_Shrine,
        Nepal_Village,
        Numbani,
        Oasis,
        Oasis_City_Center,
        Oasis_Gardens,
        Oasis_University,
        Paris,
        Petra,
        Rialto,
        Route_66,
        Sydney_Harbour_Arena,
        Temple_of_Anubis,
        Volskaya_Industries,
        [EnumOverride(null, "Watchpoint: Gibraltar")]
        Watchpoint_Gibraltar,
        Workshop_Chamber,
        Workshop_Expanse,
        [EnumOverride(null, "Workshop Expanse (Night)")]
        Workshop_Expanse_Night,
        Workshop_Island,
        [EnumOverride(null, "Workshop Island (Night)")]
        Workshop_Island_Night
    }

    [WorkshopEnum]
    public enum GameMode
    {
        Assault,
        CaptureTheFlag,
        Control,
        Deathmatch,
        Elimination,
        Escort,
        Hybrid,
        [EnumOverride(null, "Junkenstein's Revenge")]
        JunkensteinsRevenge,
        [EnumOverride(null, "Lúcioball")]
        Lucioball,
        [EnumOverride(null, "Mei's Snowball Offensive")]
        MeisSnowballOffensive,
        PracticeRange,
        Skirmish,
        TeamDeathmatch,
        YetiHunter
    }

    [WorkshopEnum]
    enum HealthType
    {
        Health,
        Armor,
        Shields
    }
}