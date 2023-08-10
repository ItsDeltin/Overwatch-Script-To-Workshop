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
    public enum RuleEvent
    {
        OngoingGlobal,
        OngoingPlayer,
        OnElimination,
        OnFinalBlow,
        OnDamageDealt,
        OnDamageTaken,
        OnDeath,
        OnHealingDealt,
        OnHealingTaken,
        OnPlayerJoin,
        OnPlayerLeave,
        Subroutine,
        PlayerDealtKnockback,
        PlayerReceivedKnockback
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
        Genji,
        Roadhog,
        Cassidy,
        Junkrat,
        Zarya,
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
        Echo,
        Baptiste,
        Sigma,
        Sojourn,
        JunkerQueen,
        Kiriko,
        Ramattra,
        Lifeweaver,
        Illari
    }

    public enum Team
    {
        All,
        Team1,
        Team2
    }

    public enum Operator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

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

    public enum Rounding
    {
        Down,
        Up,
        Nearest
    }
}
