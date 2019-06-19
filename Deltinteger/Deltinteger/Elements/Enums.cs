using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Elements
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class EnumParameter : Attribute
    {
        public EnumParameter() 
        { 

        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EnumOverride : Attribute
    {
        public EnumOverride(string codeName, string workshopName)
        {
            CodeName = codeName;
            WorkshopName = workshopName;
        }
        public string CodeName { get; private set; }
        public string WorkshopName { get; private set; }

        public static string GetWorkshopName(object enumValue)
        {
            var enumType = enumValue.GetType();
            var memInfo = enumType.GetMember(enumValue.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(EnumOverride), false);
            return ((EnumOverride)attributes.ElementAtOrDefault(0))?.WorkshopName ?? Extras.AddSpacesToSentence(enumValue.ToString(), false);
         }

         public static string[] GetCodeValues<T>()
         {
             return GetCodeValues(typeof(T));
         }

         public static string[] GetCodeValues(Type type)
         {
             return type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(v => v.GetCustomAttribute<EnumOverride>()?.CodeName ?? v.Name)
                .ToArray();
         }

         public static CompletionItem[] GetCompletion<T>()
         {
             return EnumOverride.GetCodeValues<T>().Select(value =>
                new CompletionItem(value) { kind = CompletionItem.EnumMember }
            ).ToArray();
         }
    }

    public class EnumData
    {
        private static EnumData[] AllEnums = null;

        private static EnumData[] GetEnumData()
        {
            if (AllEnums == null)
            {
                Type[] enums = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<EnumParameter>() != null).ToArray();
                AllEnums = new EnumData[enums.Length];

                for (int i = 0; i < AllEnums.Length; i++)
                {
                    EnumOverride data = enums[i].GetCustomAttribute<EnumOverride>();
                    string codeName     = data?.CodeName     ?? enums[i].Name;
                    string workshopName = data?.WorkshopName ?? enums[i].Name;

                    var fields = enums[i].GetFields(BindingFlags.Public | BindingFlags.Static);
                    EnumMember[] values = new EnumMember[fields.Length];
                    for (int v = 0; v < values.Length; v++)
                    {
                        EnumOverride fieldData = fields[v].GetCustomAttribute<EnumOverride>();
                        string fieldCodeName     = fieldData?.CodeName     ?? fields[v].Name;
                        string fieldWorkshopName = fieldData?.WorkshopName ?? fields[v].Name;

                        values[v] = new EnumMember(fieldCodeName, fieldWorkshopName);
                    }

                    AllEnums[i] = new EnumData(codeName, workshopName, values, enums[i]);
                }
            }
            return AllEnums;
        }

        public static bool IsEnum(string codeName)
        {
            return GetEnum(codeName) != null;
        }

        public static EnumData GetEnum(string codeName)
        {
            return GetEnumData().FirstOrDefault(e => e.CodeName == codeName);
        }

        public string CodeName { get; private set; }
        public string WorkshopName { get; private set; }
        public EnumMember[] Values { get; private set; }
        public Type Type { get; private set; } 

        public EnumData(string codeName, string workshopName, EnumMember[] values, Type type)
        {
            CodeName = codeName;
            WorkshopName = workshopName;
            Values = values;
            Type = type;
        }
    }

    public class EnumMember
    {
        public string CodeName { get; private set; }
        public string WorkshopName { get; private set; }
        public EnumMember(string codeName, string workshopName)
        {
            CodeName = codeName;
            WorkshopName = workshopName;
        }
    }

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
        OnDeath
    }

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
        // Why isn't it alphabetical? we will never know.
        Reaper,
        Tracer,
        Mercy,
        Hanzo,
        Torbjorn,
        Reinhardt,
        Pharah,
        Winston,
        Widowmaker,
        Bastion,
        Symmetra,
        Zenyatta,
        Gengi,
        Roadhog,
        Mccree,
        Junkrat,
        Zarya,
        [EnumOverride(null, "Soldier: 76")]
        Soldier76,
        Lucio,
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
        Baptiste
    }

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

    [EnumParameter]
    public enum Variable
    {
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z
    }

    [EnumParameter]
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

    [EnumParameter]
    public enum Button
    {
        PrimaryFire,
        SecondaryFire,
        Ability1,
        Ability2,
        Ultimate,
        Interact,
        Jump,
        Crouch
    }

    [EnumParameter]
    public enum Relative
    {
        ToWorld,
        ToPlayer
    }

    [EnumParameter]
    public enum ContraryMotion
    {
        [EnumOverride(null, "Cancel Contrary Motion")]
        Cancel,
        [EnumOverride(null, "Incorporate Contrary Motion")]
        Incorporate
    }

    [EnumParameter]
    public enum ChaseReevaluation
    {
        DestinationAndRate,
        None
    }

    [EnumParameter]
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

    [EnumParameter]
    public enum TeamSelector
    {
        All,
        Team1,
        Team2,
    }

    [EnumParameter]
    public enum WaitBehavior
    {
        IgnoreCondition,
        AbortWhenFalse,
        RestartWhenTrue
    }

    [EnumParameter]
    public enum Effects
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
        PickupSound,
        GoodAuraSound,
        BadAuraSound,
        SparklesSound,
        SmokeSound,
        DecalSound,
        BeaconSound
    }

    [EnumParameter]
    public enum Color
    {
        White,
        Yellow,
        Green,
        Purple,
        Red,
        Blue,
        Team1,
        Team2
    }

    [EnumParameter]
    public enum EffectRev
    {
        VisibleToPositionAndRadius,
        PositionAndRadius,
        VisibleTo,
        None
    }

    [EnumParameter]
    public enum Rounding
    {
        Up,
        Down,
        Nearest
    }

    [EnumParameter]
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
        Acknowledge
    }

    [EnumParameter]
    public enum Location
    {
        Left,
        Top,
        Right
    }

    [EnumParameter]
    public enum StringRev
    {
        VisibleToAndString,
        String
    }

    [EnumParameter]
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
        Aterisk,
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
        Poison1,
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

    [EnumParameter]
    public enum IconRev
    {
        VisibleToAndPosition,
        Position,
        VisibleTo,
        None
    }

    [EnumParameter]
    public enum PlayEffects
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

    [EnumParameter]
    public enum Hero
    {
        Ana,
        Ashe,
        Baptiste,
        Bastion,
        Brigitte,
        Dva,
        Doomfist,
        Genji,
        Hanzo,
        Junkrat,
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
        [EnumOverride(null, "Soldier: 76")]
        Soldier76,
        Sombra,
        Symmetra,
        Torbjorn,
        Tracer,
        Widowmaker,
        Winston,
        WreckingBall,
        Zarya,
        Zenyatta
    }

    [EnumParameter]
    public enum InvisibleTo
    {
        All,
        Enemies,
        None
    }

    [EnumParameter]
    public enum AccelerateRev
    {
        DirectionRateAndMaxSpeed,
        None
    }

    [EnumParameter]
    public enum ModRev
    {
        ReceiversDamagersAndDamagePercent,
        ReceiversAndDamagers,
        None
    }

    [EnumParameter]
    public enum FacingRev
    {
        DirectionAndTurnRate,
        None
    }

    [EnumParameter]
    public enum BarrierLOS
    {
        [EnumOverride(null, "Barriers Do Not Block LOS")]
        NoBarriersBlock,
        [EnumOverride(null, "Enemy Barriers Block LOS")]
        EnemyBarriersBlock,
        [EnumOverride(null, "All Barriers Block LOS")]
        AllBarriersBlock
    }

    [EnumParameter]
    public enum Transformation
    {
        Rotation,
        RotationAndTranslation
    }

    [EnumParameter]
    public enum RadiusLOS
    {
        Off,
        Surfaces,
        SurfacesAndEnemyBarriers,
        SurfacesAndAllBarriers
    }

    [EnumParameter]
    public enum LocalVector
    {
        Rotation,
        RotationAndTranslation
    }

    [EnumParameter]
    public enum Clipping
    {
        ClipAgainstSurfaces,
        DoNotClip
    }

    [EnumParameter]
    public enum InworldTextRev
    {
        VisibleToPositionAndString,
        VisibleToAndString,
        String
    }
}
