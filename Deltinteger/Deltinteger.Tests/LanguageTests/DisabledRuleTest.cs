namespace Deltinteger.Tests;

[TestClass]
public class LanguageTest
{
    [TestMethod("Disabled Rules")]
    public void DisabledRules()
    {
        Compile(
            """
            disabled rule: "" {
                Number a = 1;
            }
            rule: "" {
                Number b = 2;
            }
            """
        ).AssertOk().EmulateTick().AssertVariable("a", 0).AssertVariable("b", 2);
    }

    [TestMethod("Disabled Conditions")]
    public void DisabledConditions()
    {
        Compile(
            """
            rule: ""
            if (false)
            {
                Number a = 1;
            }

            rule: ""
            disabled if (false)
            {
                Number b = 2;
            }
            """
        ).AssertOk().EmulateTick().AssertVariable("a", 0).AssertVariable("b", 2);
    }
}