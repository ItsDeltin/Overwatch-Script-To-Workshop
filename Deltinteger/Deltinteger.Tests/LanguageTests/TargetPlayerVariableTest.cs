namespace Deltinteger.Tests;

// Some constructs in OSTW must extract player variable data from an
// expression to properly compile workshop actions that have the target
// player as a parameter. These tests go through a variety of situations
// where this scenario comes up.

[TestClass]
public class TargetPlayerVariableTest
{
    [TestMethod("Chase Event Player variable at rate")]
    public void ChaseEventPlayerVariableAtRate()
    {
        Compile(
            """
            playervar Number a;

            rule: ""
            Event.OngoingPlayer
            {
                ChaseVariableAtRate(a, 1, 2, RateChaseReevaluation.DestinationAndRate);
            }
            """
        ).AssertOk().AssertText("Chase Player Variable At Rate(Event Player, a, 1, 2, Destination And Rate);");
    }

    [TestMethod("Chase Host variable at rate")]
    public void ChaseHostVariableAtRate()
    {
        Compile(
            """
            playervar Number a;

            rule: "" {
                ChaseVariableAtRate(HostPlayer().a, 1, 2, RateChaseReevaluation.DestinationAndRate);
            }
            """
        ).AssertOk().AssertText("Chase Player Variable At Rate(Host Player, a, 1, 2, Destination And Rate);");
    }

    [TestMethod("Chase Event Player variable over time")]
    public void ChaseEventPlayerVariableOverTime()
    {
        Compile(
            """
            playervar Number a;

            rule: ""
            Event.OngoingPlayer
            {
                ChaseVariableOverTime(a, 1, 2, TimeChaseReevaluation.DestinationAndDuration);
            }
            """
        ).AssertOk().AssertText("Chase Player Variable Over Time(Event Player, a, 1, 2, Destination And Duration);");
    }

    [TestMethod("Chase Host variable over time")]
    public void ChaseHostVariableOverTime()
    {
        Compile(
            """
            playervar Number a;

            rule: "" {
                ChaseVariableOverTime(HostPlayer().a, 1, 2, TimeChaseReevaluation.DestinationAndDuration);
            }
            """
        ).AssertOk().AssertText("Chase Player Variable Over Time(Host Player, a, 1, 2, Destination And Duration);");
    }

    [TestMethod("Stop chasing Event Player variable")]
    public void StopChasingEventPlayerVariable()
    {
        Compile(
            """
            playervar Number a;

            rule: ""
            Event.OngoingPlayer
            {
                StopChasingVariable(a);
            }
            """
        ).AssertOk().AssertText("Stop Chasing Player Variable(Event Player, a);");
    }

    [TestMethod("Stop chasing Host Player variable")]
    public void StopChasingHostVariable()
    {
        Compile(
            """
            playervar Number a;

            rule: "" {
                StopChasingVariable(HostPlayer().a);
            }
            """
        ).AssertOk().AssertText("Stop Chasing Player Variable(Host Player, a);");
    }

    [TestMethod("Auto-For Event Player")]
    public void AutoForEventPlayer()
    {
        Compile(
            """
            playervar Number a;

            rule: ""
            Event.OngoingPlayer
            {
                for (a = 0; 1; 1) {}
            }
            """
        ).AssertOk().AssertText("For Player Variable(Event Player, a, 0, 1, 1);");
    }


    [TestMethod("Auto-For Host")]
    public void AutoForHost()
    {
        Compile(
            """
            playervar Number a;

            rule: "" {
                for (HostPlayer().a = 0; 1; 1) {}
            }
            """
        ).AssertOk().AssertText("For Player Variable(Host Player, a, 0, 1, 1);");
    }

    [TestMethod("Modify Event Player variable")]
    public void ModifyEventPlayerVariable()
    {
        Compile("""
        playervar Number array;

        rule: ""
        Event.OngoingPlayer
        {
            ModifyVariable(array, Operation.Max, 3);
        }
        """).AssertOk().AssertText("Modify Player Variable(Event Player, array, Max, 3);");
    }

    [TestMethod("Modify Host variable")]
    public void ModifyHostVariable()
    {
        Compile("""
        playervar Number array;

        rule: ""
        {
            ModifyVariable(HostPlayer().array, Operation.Max, 3);
        }
        """).AssertOk().AssertText("Modify Player Variable(Host Player, array, Max, 3);");
    }

    [TestMethod("Target through ref parameter")]
    public void TargetThroughRefParameter()
    {
        Compile("""
        playervar Number a;

        rule: "" {
            func(HostPlayer().a);
        }

        void func(ref Number t) {
            t = 3;
        }
        """).AssertOk().AssertText("Host Player.a = 3;");
    }

    [TestMethod("Target player variable through struct")]
    public void TargetPlayerVariableThroughPlayerStruct()
    {
        Compile(
            """
            struct TestStruct { public Player target; }
            playervar Number num;
            globalvar TestStruct gvar;
            playervar TestStruct pvar;

            rule: "" {
                ChaseVariableAtRate(gvar.target.num, 1, 1, RateChaseReevaluation.DestinationAndRate);
                ChaseVariableOverTime(gvar.target.num, 1, 1, TimeChaseReevaluation.DestinationAndDuration);
                StopChasingVariable(gvar.target.num);

                ChaseVariableAtRate(HostPlayer().pvar.target.num, 2, 2, RateChaseReevaluation.DestinationAndRate);
                ChaseVariableOverTime(HostPlayer().pvar.target.num, 2, 2, TimeChaseReevaluation.DestinationAndDuration);
                StopChasingVariable(HostPlayer().pvar.target.num);

                ModifyVariable(gvar.target.num, Operation.Max, 3);
                ModifyVariable(HostPlayer().pvar.target.num, Operation.Max, 3);

                for (gvar.target.num; 1; 1) {}
                for (HostPlayer().pvar.target.num; 1; 1) {}
            }
            """
        ).AssertOk()
        .AssertText("Chase Player Variable At Rate(Global.gvar_target, num, 1, 1, Destination And Rate);")
        .AssertText("Chase Player Variable Over Time(Global.gvar_target, num, 1, 1, Destination And Duration);")
        .AssertText("Stop Chasing Player Variable(Global.gvar_target, num);")

        .AssertText("Chase Player Variable At Rate(Host Player.pvar_target, num, 2, 2, Destination And Rate);")
        .AssertText("Chase Player Variable Over Time(Host Player.pvar_target, num, 2, 2, Destination And Duration);")
        .AssertText("Stop Chasing Player Variable(Host Player.pvar_target, num);")

        .AssertText("Modify Player Variable(Global.gvar_target, num, Max, 3);")
        .AssertText("Modify Player Variable(Host Player.pvar_target, num, Max, 3);")

        .AssertText("For Player Variable(Global.gvar_target, num, 0, 1, 1);")
        .AssertText("For Player Variable(Host Player.pvar_target, num, 0, 1, 1);");
    }
}