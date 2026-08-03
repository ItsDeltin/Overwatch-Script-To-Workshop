namespace Deltinteger.Tests;

[TestClass]
public class RuleTest
{
    [TestMethod("Invalid condition value type")]
    public void InvalidConditionValueType()
    {
        Compile(
            """
            rule: ""
            if ({ A: 1 })
            {}
            """)
        .AssertSearchError("The value of a condition cannot be a constant or parallel value");

        Compile(
            """
            rule: ""
            if (EffectRev.Color)
            {}
            """)
        .AssertSearchError("The value of a condition cannot be a constant or parallel value");
    }
}