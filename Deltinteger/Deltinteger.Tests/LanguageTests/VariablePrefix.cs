namespace Deltinteger.Tests;

[TestClass]
public class VariablePrefixTest
{
    [TestMethod("Test variable prefixes")]
    public void TestVariablePrefixes()
    {
        var script = """
        globalvar Number b = 3;
        globalvar Number test_c = 4;
        """;

        Compile(script).EmulateTick()
            .AssertVariable("b", 3).AssertVariable("test_c", 4);
        Compile(script, variablePrefix: "test_")
            .EmulateTick().AssertVariable("test_b", 3).AssertVariable("test_c", 4);
    }
}