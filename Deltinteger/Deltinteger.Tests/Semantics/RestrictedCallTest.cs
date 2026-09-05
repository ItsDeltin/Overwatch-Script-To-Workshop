namespace Deltinteger.Tests;

[TestClass]
public class RestrictedCallTest
{
    [TestMethod("Restricted call test - Actions in conditions")]
    public void UseActionsInConditions()
    {
        // With 'in' parameter and no other actions.
        Compile("""
        Any func(in Any parameter) {
            Any inline: parameter + 1;
            return inline;
        }

        rule: ""
        if (func(0)) {}
        """)
        .AssertOk();

        // With parameter that generates a variable.
        Compile("""
        Any func(Any parameter) {
            Any inline: parameter + 1;
            return inline;
        }

        rule: ""
        if (func(0)) {}
        """)
        .AssertSearchError("cannot be used in a condition because it generates actions");

        // With function that creates actions by declaring a variable.
        Compile("""
        Any func() {
            Any var = 1;
            return var;
        }

        rule: ""
        if (func()) {}
        """)
        .AssertSearchError("cannot be used in a condition because it generates actions");
    }

    [TestMethod("Restricted call test - Invalid element")]
    public void InvalidElement()
    {
        // Invalid element usage.
        Compile("""
        rule: "" {
            define a = EventPlayer();
            define b = Attacker();
            define c = Healer();
            define d = EventAbility();
        }
        """)
        .AssertSearchError("A restricted value of type 'Event Player' cannot be called in this rule")
        .AssertSearchError("A restricted value of type 'Attacker' cannot be called in this rule")
        .AssertSearchError("A restricted value of type 'Healer' cannot be called in this rule")
        .AssertSearchError("A restricted value of type 'Ability' cannot be called in this rule");
    }

    [TestMethod("Restricted call test - Unset optional parameter")]
    public void UnsetOptionalParameter()
    {
        // Error from unset parameter.
        Compile("""
        void func(Player x = EventPlayer()) {}

        rule: "" {
            func();
            SetMaxHealth();
        }
        """)
        .AssertSearchError("An unset optional parameter 'x' in the function 'func([Player x])' calls a restricted value of type 'Event Player'")
        .AssertSearchError("An unset optional parameter 'Player' in the function 'SetMaxHealth([Player | Player[] Player], [Number HealthPercent])' calls a restricted value of type 'Event Player'");

        // No error from set parameter.
        Compile("""
        void func(Player x = EventPlayer()) {}

        rule: "" {
            func(HostPlayer());
            SetMaxHealth(HostPlayer());
        }
        """)
        .AssertOk();
    }

    [TestMethod("Restricted call test - No player provided for player variable")]
    public void NoPlayerProvidedForPlayerVariable()
    {
        Compile("""
        playervar Any var1;
        playervar MyStruct var2; 

        struct MyStruct { public Any value; }

        rule: "" {
            var1 = 5;
            var2.value = 5;

            func();
        }

        void func() {
            var1 = 5;
        }
        """)
        .AssertSearchError("The variable 'var1' is a player variable and no player was provided in a global rule")
        .AssertSearchError("The variable 'var2' is a player variable and no player was provided in a global rule")
        .AssertSearchError("The function 'func' calls a restricted value of type 'Event Player'");
    }
}