using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.OverwatchParser.Elements
{
    public static class Constants
    {
        public static readonly string[] Strings = new string[]
        {
            // No uppercase
            " ",
            "----------",
            "-> {0}",
            "!",
            "!!",
            "!!!",
            "#{0}",
            "({0})",
            "*",
            "...",
            "?",
            "??",
            "???",
            "{0} - {1}",
            "{0} - {1} - {2}",
            "{0} ->",
            "{0} -> {1}",
            "{0} != {1}",
            "{0} * {1}",
            "{0} / {1}",
            "{0} : {1} : {2}",
            "{0} {1}",
            "{0} {1} {2}"
        };
    }

    public enum Relative
    {
        ToWorld,
        ToPlayer
    }

    public enum ContraryMotion
    {
        Cancel,
        Incorporate
    }

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

    public enum ChaseReevaluation
    {
        DestinationAndRate,
        None
    }

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
}
